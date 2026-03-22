using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.OperationalIntelligence.Contracts.Cost.ServiceInterfaces;

namespace NexTraceOne.Governance.Application.Features.GetFinOpsTrends;

/// <summary>
/// Feature: GetFinOpsTrends — tendências de custo por dimensão (serviço, equipa, domínio).
/// Tendências no NexTraceOne são contextualizadas e ligadas a comportamento operacional.
/// Consome dados reais do módulo CostIntelligence via contrato público.
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
        private readonly ICostIntelligenceModule _costModule;

        public Handler(ICostIntelligenceModule costModule)
        {
            _costModule = costModule;
        }

        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var records = await _costModule.GetCostRecordsAsync(cancellationToken: cancellationToken);

            var grouped = request.Dimension switch
            {
                CostDimension.Team => records.GroupBy(r => (Id: r.Team ?? string.Empty, Name: r.Team ?? string.Empty)),
                CostDimension.Domain => records.GroupBy(r => (Id: r.Domain ?? string.Empty, Name: r.Domain ?? string.Empty)),
                _ => records.GroupBy(r => (Id: r.ServiceId, Name: r.ServiceName))
            };

            if (!string.IsNullOrWhiteSpace(request.FilterId))
                grouped = grouped.Where(g => g.Key.Id.Equals(request.FilterId, StringComparison.OrdinalIgnoreCase));

            var series = grouped
                .Select(g =>
                {
                    var points = g
                        .GroupBy(r => r.Period)
                        .OrderBy(p => p.Key)
                        .Select(p => new TrendDataPointDto(p.Key, p.Sum(r => r.TotalCost)))
                        .ToList();

                    var changePercent = points.Count >= 2 && points[0].Cost != 0m
                        ? (points[^1].Cost - points[0].Cost) / points[0].Cost * 100m
                        : 0m;

                    var direction = changePercent switch
                    {
                        > 5m => TrendDirection.Declining,
                        < -5m => TrendDirection.Improving,
                        _ => TrendDirection.Stable
                    };

                    return new TrendSeriesDto(g.Key.Id, g.Key.Name, points, direction, Math.Round(changePercent, 1));
                })
                .ToList();

            var aggregated = records
                .GroupBy(r => r.Period)
                .OrderBy(p => p.Key)
                .Select(p => new TrendDataPointDto(p.Key, p.Sum(r => r.TotalCost)))
                .ToList();

            var overallChange = aggregated.Count >= 2 && aggregated[0].Cost != 0m
                ? (aggregated[^1].Cost - aggregated[0].Cost) / aggregated[0].Cost * 100m
                : 0m;

            var overallDirection = overallChange switch
            {
                > 5m => TrendDirection.Declining,
                < -5m => TrendDirection.Improving,
                _ => TrendDirection.Stable
            };

            var response = new Response(
                Dimension: request.Dimension,
                Series: series,
                AggregatedTrend: aggregated,
                OverallDirection: overallDirection,
                OverallChangePercent: Math.Round(overallChange, 1),
                GeneratedAt: DateTimeOffset.UtcNow,
                IsSimulated: false,
                DataSource: "cost-intelligence");

            return Result<Response>.Success(response);
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
