using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.ListCostAttributions;

/// <summary>
/// Feature: ListCostAttributions — lista atribuições de custo operacional por dimensão,
/// com filtros opcionais de período.
///
/// Owner: módulo Governance.
/// Pilar: FinOps contextual — visão panorâmica de custos por dimensão operacional.
/// </summary>
public static class ListCostAttributions
{
    /// <summary>Query para listar atribuições de custo por dimensão, com filtros opcionais de período.</summary>
    public sealed record Query(
        CostAttributionDimension Dimension,
        DateTimeOffset? PeriodStart = null,
        DateTimeOffset? PeriodEnd = null) : IQuery<Response>;

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Dimension).IsInEnum();
            RuleFor(x => x.PeriodEnd)
                .GreaterThan(x => x.PeriodStart)
                .When(x => x.PeriodStart.HasValue && x.PeriodEnd.HasValue);
        }
    }

    /// <summary>Handler que lista atribuições de custo por dimensão com filtros opcionais.</summary>
    public sealed class Handler(
        ICostAttributionRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var attributions = await repository.ListByDimensionAsync(
                request.Dimension, request.PeriodStart, request.PeriodEnd, cancellationToken);

            var items = attributions
                .Select(a => new CostAttributionItemDto(
                    AttributionId: a.Id.Value,
                    Dimension: a.Dimension,
                    DimensionKey: a.DimensionKey,
                    DimensionLabel: a.DimensionLabel,
                    PeriodStart: a.PeriodStart,
                    PeriodEnd: a.PeriodEnd,
                    TotalCost: a.TotalCost,
                    ComputeCost: a.ComputeCost,
                    StorageCost: a.StorageCost,
                    NetworkCost: a.NetworkCost,
                    OtherCost: a.OtherCost,
                    Currency: a.Currency,
                    AttributionMethod: a.AttributionMethod,
                    ComputedAt: a.ComputedAt))
                .ToList();

            return Result<Response>.Success(new Response(
                Items: items,
                TotalCount: items.Count,
                FilteredDimension: request.Dimension));
        }
    }

    /// <summary>Resposta com a lista de atribuições de custo para a dimensão.</summary>
    public sealed record Response(
        IReadOnlyList<CostAttributionItemDto> Items,
        int TotalCount,
        CostAttributionDimension FilteredDimension);

    /// <summary>DTO resumido de uma atribuição de custo para listagem.</summary>
    public sealed record CostAttributionItemDto(
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
        string? AttributionMethod,
        DateTimeOffset ComputedAt);
}
