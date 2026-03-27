using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.RegisterSloDefinition;

/// <summary>
/// Feature: RegisterSloDefinition — regista uma nova definição de SLO para um serviço num ambiente.
/// </summary>
public static class RegisterSloDefinition
{
    public sealed record Command(
        string ServiceId,
        string Environment,
        string Name,
        SloType Type,
        decimal TargetPercent,
        int WindowDays,
        string? Description = null,
        decimal? AlertThresholdPercent = null) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Type).IsInEnum();
            RuleFor(x => x.TargetPercent).InclusiveBetween(0m, 100m);
            RuleFor(x => x.WindowDays).GreaterThan(0);
            RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
            RuleFor(x => x.AlertThresholdPercent)
                .InclusiveBetween(0m, 100m)
                .When(x => x.AlertThresholdPercent.HasValue);
        }
    }

    public sealed class Handler(
        ISloDefinitionRepository repository,
        ICurrentTenant tenant) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var slo = SloDefinition.Create(
                tenant.Id,
                request.ServiceId,
                request.Environment,
                request.Name,
                request.Type,
                request.TargetPercent,
                request.WindowDays,
                request.Description,
                request.AlertThresholdPercent);

            await repository.AddAsync(slo, cancellationToken);

            return Result<Response>.Success(new Response(slo.Id.Value, slo.Name, slo.ServiceId, slo.Environment, slo.Type, slo.TargetPercent, slo.WindowDays));
        }
    }

    public sealed record Response(
        Guid Id,
        string Name,
        string ServiceId,
        string Environment,
        SloType Type,
        decimal TargetPercent,
        int WindowDays);
}
