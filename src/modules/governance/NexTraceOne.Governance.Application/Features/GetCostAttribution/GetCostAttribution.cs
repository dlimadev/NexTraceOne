using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.Governance.Domain.Errors;

namespace NexTraceOne.Governance.Application.Features.GetCostAttribution;

/// <summary>
/// Feature: GetCostAttribution — obtém uma atribuição de custo operacional pelo seu identificador.
///
/// Owner: módulo Governance.
/// Pilar: FinOps contextual — consulta de atribuição de custo individual.
/// </summary>
public static class GetCostAttribution
{
    /// <summary>Query para obter uma atribuição de custo pelo identificador.</summary>
    public sealed record Query(Guid AttributionId) : IQuery<Response>;

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.AttributionId).NotEmpty();
        }
    }

    /// <summary>Handler que obtém uma atribuição de custo pelo seu identificador.</summary>
    public sealed class Handler(
        ICostAttributionRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var attribution = await repository.GetByIdAsync(
                new Domain.Entities.CostAttributionId(request.AttributionId), cancellationToken);

            if (attribution is null)
                return GovernanceCostAttributionErrors.AttributionNotFound(request.AttributionId.ToString());

            return Result<Response>.Success(new Response(
                AttributionId: attribution.Id.Value,
                Dimension: attribution.Dimension,
                DimensionKey: attribution.DimensionKey,
                DimensionLabel: attribution.DimensionLabel,
                PeriodStart: attribution.PeriodStart,
                PeriodEnd: attribution.PeriodEnd,
                TotalCost: attribution.TotalCost,
                ComputeCost: attribution.ComputeCost,
                StorageCost: attribution.StorageCost,
                NetworkCost: attribution.NetworkCost,
                OtherCost: attribution.OtherCost,
                Currency: attribution.Currency,
                CostBreakdown: attribution.CostBreakdown,
                AttributionMethod: attribution.AttributionMethod,
                DataSources: attribution.DataSources,
                ComputedAt: attribution.ComputedAt));
        }
    }

    /// <summary>Resposta completa com todos os detalhes de uma atribuição de custo.</summary>
    public sealed record Response(
        Guid AttributionId,
        CostAttributionDimension Dimension,
        string DimensionKey,
        string? DimensionLabel,
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd,
        decimal TotalCost,
        decimal ComputeCost,
        decimal StorageCost,
        decimal NetworkCost,
        decimal OtherCost,
        string Currency,
        string? CostBreakdown,
        string? AttributionMethod,
        string? DataSources,
        DateTimeOffset ComputedAt);
}
