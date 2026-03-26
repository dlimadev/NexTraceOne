using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.RegisterSlaDefinition;

/// <summary>
/// Feature: RegisterSlaDefinition — regista uma nova definição de SLA associada a um SLO existente.
/// </summary>
public static class RegisterSlaDefinition
{
    public sealed record Command(
        Guid SloDefinitionId,
        string Name,
        decimal ContractualTargetPercent,
        DateTimeOffset EffectiveFrom,
        string? Description = null,
        DateTimeOffset? EffectiveTo = null,
        bool HasPenaltyClauses = false,
        string? PenaltyNotes = null) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.SloDefinitionId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ContractualTargetPercent).InclusiveBetween(0m, 100m);
            RuleFor(x => x.EffectiveFrom).NotEmpty();
            RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
            RuleFor(x => x.PenaltyNotes).MaximumLength(2000).When(x => x.PenaltyNotes is not null);
        }
    }

    public sealed class Handler(
        ISloDefinitionRepository sloRepository,
        ISlaDefinitionRepository slaRepository,
        ICurrentTenant tenant) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var sloId = SloDefinitionId.From(request.SloDefinitionId);
            var slo = await sloRepository.GetByIdAsync(sloId, tenant.Id, cancellationToken);

            if (slo is null)
            {
                Result<Response> notFound = Error.NotFound("Reliability.SloNotFound",
                    "SLO definition '{0}' not found", request.SloDefinitionId);
                return notFound;
            }

            var sla = SlaDefinition.Create(
                tenant.Id,
                sloId,
                request.Name,
                request.ContractualTargetPercent,
                request.EffectiveFrom,
                request.Description,
                request.EffectiveTo,
                request.HasPenaltyClauses,
                request.PenaltyNotes);

            await slaRepository.AddAsync(sla, cancellationToken);

            return Result<Response>.Success(new Response(sla.Id.Value, sla.Name, slo.Name, sla.ContractualTargetPercent, sla.Status));
        }
    }

    public sealed record Response(
        Guid Id,
        string Name,
        string SloName,
        decimal ContractualTargetPercent,
        SlaStatus Status);
}
