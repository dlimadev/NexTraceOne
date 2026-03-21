using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetFinOpsTrends;

/// <summary>
/// Feature: GetFinOpsTrends — tendências de custo por dimensão (serviço, equipa, domínio).
/// Tendências no NexTraceOne são contextualizadas e ligadas a comportamento operacional.
/// IMPLEMENTATION STATUS: Demo — returns illustrative data. Will be replaced by real
/// cost snapshot trend computation from CostIntelligence in a future sprint.
/// </summary>
public static class GetFinOpsTrends
{
    /// <summary>Query para obter tendências de custo.</summary>
    public sealed record Query(
        CostDimension Dimension = CostDimension.Service,
        string? FilterId = null) : IQuery<Response>;

    /// <summary>Handler que retorna tendências de custo.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var series = new List<TrendSeriesDto>
            {
                new("svc-order-processor", "Order Processor", new TrendDataPointDto[]
                {
                    new("2025-10", 16200m), new("2025-11", 17100m),
                    new("2025-12", 17800m), new("2026-01", 18100m),
                    new("2026-02", 18400m), new("2026-03", 18700m)
                }, TrendDirection.Declining, 15.4m),
                new("svc-payment-api", "Payment API", new TrendDataPointDto[]
                {
                    new("2025-10", 10800m), new("2025-11", 11200m),
                    new("2025-12", 11500m), new("2026-01", 11800m),
                    new("2026-02", 12100m), new("2026-03", 12500m)
                }, TrendDirection.Declining, 15.7m),
                new("svc-notification-hub", "Notification Hub", new TrendDataPointDto[]
                {
                    new("2025-10", 2200m), new("2025-11", 2100m),
                    new("2025-12", 2000m), new("2026-01", 1950m),
                    new("2026-02", 1900m), new("2026-03", 1800m)
                }, TrendDirection.Improving, -18.2m),
                new("svc-user-service", "User Service", new TrendDataPointDto[]
                {
                    new("2025-10", 4100m), new("2025-11", 4150m),
                    new("2025-12", 4200m), new("2026-01", 4180m),
                    new("2026-02", 4200m), new("2026-03", 4200m)
                }, TrendDirection.Stable, 2.4m)
            };

            var aggregated = new List<TrendDataPointDto>
            {
                new("2025-10", 55200m), new("2025-11", 57100m), new("2025-12", 58500m),
                new("2026-01", 59800m), new("2026-02", 60500m), new("2026-03", 61300m)
            };

            var response = new Response(
                Dimension: request.Dimension,
                Series: series,
                AggregatedTrend: aggregated,
                OverallDirection: TrendDirection.Declining,
                OverallChangePercent: 11.1m,
                GeneratedAt: DateTimeOffset.UtcNow,
                IsSimulated: true,
                DataSource: "demo");

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com tendências de custo. IsSimulated=true indica dados demonstrativos.</summary>
    public sealed record Response(
        CostDimension Dimension,
        IReadOnlyList<TrendSeriesDto> Series,
        IReadOnlyList<TrendDataPointDto> AggregatedTrend,
        TrendDirection OverallDirection,
        decimal OverallChangePercent,
        DateTimeOffset GeneratedAt,
        bool IsSimulated = false,
        string? DataSource = null);

    /// <summary>Série temporal de custo para uma entidade.</summary>
    public sealed record TrendSeriesDto(
        string EntityId,
        string EntityName,
        IReadOnlyList<TrendDataPointDto> DataPoints,
        TrendDirection Direction,
        decimal ChangePercent);

    /// <summary>Ponto de dados de tendência.</summary>
    public sealed record TrendDataPointDto(
        string Period,
        decimal Cost);
}
