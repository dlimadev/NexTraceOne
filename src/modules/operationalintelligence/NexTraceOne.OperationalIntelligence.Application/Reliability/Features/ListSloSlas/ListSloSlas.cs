using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.ListSloSlas;

/// <summary>
/// Feature: ListSloSlas — lista todas as definições de SLA associadas a um SLO.
/// </summary>
public static class ListSloSlas
{
    public sealed record Query(Guid SloDefinitionId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.SloDefinitionId).NotEmpty();
        }
    }

    public sealed class Handler(
        ISloDefinitionRepository sloRepository,
        ISlaDefinitionRepository slaRepository,
        ICurrentTenant tenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var sloId = SloDefinitionId.From(request.SloDefinitionId);
            var slo = await sloRepository.GetByIdAsync(sloId, tenant.Id, cancellationToken);

            if (slo is null)
            {
                Result<Response> notFound = Error.NotFound("Reliability.SloNotFound",
                    "SLO definition '{0}' not found", request.SloDefinitionId);
                return notFound;
            }

            var slas = await slaRepository.GetBySloAsync(sloId, tenant.Id, cancellationToken);

            var items = slas.Select(s => new SlaItem(
                s.Id.Value,
                s.Name,
                s.ContractualTargetPercent,
                s.Status,
                s.EffectiveFrom,
                s.EffectiveTo,
                s.HasPenaltyClauses,
                s.IsActive)).ToList();

            return Result<Response>.Success(new Response(slo.Id.Value, slo.Name, items));
        }
    }

    public sealed record SlaItem(
        Guid Id,
        string Name,
        decimal ContractualTargetPercent,
        SlaStatus Status,
        DateTimeOffset EffectiveFrom,
        DateTimeOffset? EffectiveTo,
        bool HasPenaltyClauses,
        bool IsActive);

    public sealed record Response(
        Guid SloDefinitionId,
        string SloName,
        IReadOnlyList<SlaItem> Items);
}
