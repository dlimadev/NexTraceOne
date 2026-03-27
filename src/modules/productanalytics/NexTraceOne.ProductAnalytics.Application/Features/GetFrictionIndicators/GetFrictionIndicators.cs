using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ProductAnalytics.Application.Abstractions;
using NexTraceOne.ProductAnalytics.Domain.Enums;

namespace NexTraceOne.ProductAnalytics.Application.Features.GetFrictionIndicators;

/// <summary>
/// Retorna indicadores de fricção do produto baseados em dados reais de analytics.
/// Responde: onde os utilizadores encontram mais dificuldade?
/// Consome dados reais do IAnalyticsEventRepository.
/// Heurística: contagem de eventos de tipo fricção (ZeroResultSearch, EmptyState, JourneyAbandoned)
/// agrupados por tipo e comparados com período anterior para determinar tendência.
/// </summary>
public static class GetFrictionIndicators
{
    /// <summary>Query para indicadores de fricção.</summary>
    public sealed record Query(
        string? Persona,
        string? Module,
        string? Range) : IQuery<Response>;

    /// <summary>Handler que calcula e retorna indicadores de fricção baseados em dados reais de analytics.</summary>
    public sealed class Handler(IAnalyticsEventRepository analyticsRepo) : IQueryHandler<Query, Response>
    {
        private static readonly (AnalyticsEventType EventType, FrictionSignalType SignalType, string Name)[] FrictionEvents =
        [
            (AnalyticsEventType.ZeroResultSearch, FrictionSignalType.ZeroResultSearch, "Zero Result Searches"),
            (AnalyticsEventType.EmptyStateEncountered, FrictionSignalType.RepeatedEmptyState, "Empty State Encountered"),
            (AnalyticsEventType.JourneyAbandoned, FrictionSignalType.AbortedJourney, "Journey Abandoned")
        ];

        private static TrendDirection ClassifyTrend(long current, long previous)
        {
            if (previous == 0) return TrendDirection.Stable;
            return current < (long)(previous * 0.95m)
                ? TrendDirection.Improving
                : current > (long)(previous * 1.05m)
                    ? TrendDirection.Declining
                    : TrendDirection.Stable;
        }

        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var range = request.Range ?? "last_30d";
            var (from, to) = ParseRange(range);
            var previousFrom = from.AddDays(-(to - from).TotalDays);

            var totalEvents = await analyticsRepo.CountAsync(
                request.Persona, null, null, null, from, to, cancellationToken);

            if (totalEvents == 0)
            {
                return Result<Response>.Success(new Response(
                    Indicators: Array.Empty<FrictionIndicatorDto>(),
                    OverallFrictionScore: 0m,
                    HighestFrictionModule: ProductModule.Dashboard,
                    MostCommonSignal: FrictionSignalType.ZeroResultSearch,
                    ImprovingSignals: 0,
                    DecliningSignals: 0,
                    StableSignals: 0,
                    PeriodLabel: range,
                    IsSimulated: false,
                    DataSource: "analytics"));
            }

            var indicators = new List<FrictionIndicatorDto>();

            foreach (var (eventType, signalType, name) in FrictionEvents)
            {
                var count = await analyticsRepo.CountByEventTypeAsync(
                    eventType, request.Persona, from, to, cancellationToken);

                if (count == 0) continue;

                var previousCount = await analyticsRepo.CountByEventTypeAsync(
                    eventType, request.Persona, previousFrom, from, cancellationToken);

                var trend = ClassifyTrend(count, previousCount);

                var impactPercent = totalEvents > 0
                    ? Math.Round((decimal)count / totalEvents * 100, 1)
                    : 0m;

                var topModules = await analyticsRepo.GetTopModulesAsync(
                    request.Persona, null, null, from, to, 1, cancellationToken);
                var module = topModules.Count > 0 ? topModules[0].Module : ProductModule.Dashboard;

                indicators.Add(new FrictionIndicatorDto(
                    signalType,
                    name,
                    module,
                    impactPercent,
                    (int)count,
                    trend,
                    $"{count} occurrences in period ({impactPercent}% of total events)"));
            }

            if (!string.IsNullOrWhiteSpace(request.Module) &&
                Enum.TryParse<ProductModule>(request.Module, true, out var moduleFilter))
            {
                indicators = indicators.Where(i => i.Module == moduleFilter).ToList();
            }

            var improving = indicators.Count(i => i.Trend == TrendDirection.Improving);
            var declining = indicators.Count(i => i.Trend == TrendDirection.Declining);
            var stable = indicators.Count(i => i.Trend == TrendDirection.Stable);

            var overallFriction = indicators.Count > 0
                ? indicators.Average(i => i.ImpactPercent)
                : 0m;

            var highestModule = indicators.Count > 0
                ? indicators.OrderByDescending(i => i.OccurrenceCount).First().Module
                : ProductModule.Dashboard;

            var mostCommon = indicators.Count > 0
                ? indicators.OrderByDescending(i => i.OccurrenceCount).First().SignalType
                : FrictionSignalType.ZeroResultSearch;

            return Result<Response>.Success(new Response(
                Indicators: indicators,
                OverallFrictionScore: Math.Round(overallFriction, 1),
                HighestFrictionModule: highestModule,
                MostCommonSignal: mostCommon,
                ImprovingSignals: improving,
                DecliningSignals: declining,
                StableSignals: stable,
                PeriodLabel: range,
                IsSimulated: false,
                DataSource: "analytics"));
        }

        private static (DateTimeOffset From, DateTimeOffset To) ParseRange(string range)
        {
            var now = DateTimeOffset.UtcNow;
            var days = range switch
            {
                "last_1d" => 1,
                "last_7d" => 7,
                "last_90d" => 90,
                _ => 30
            };
            return (now.AddDays(-days), now);
        }
    }

    /// <summary>Resposta com indicadores de fricção baseados em dados reais de analytics.</summary>
    public sealed record Response(
        IReadOnlyList<FrictionIndicatorDto> Indicators,
        decimal OverallFrictionScore,
        ProductModule HighestFrictionModule,
        FrictionSignalType MostCommonSignal,
        int ImprovingSignals,
        int DecliningSignals,
        int StableSignals,
        string PeriodLabel,
        bool IsSimulated = false,
        string? DataSource = null);

    /// <summary>Indicador de fricção individual.</summary>
    public sealed record FrictionIndicatorDto(
        FrictionSignalType SignalType,
        string SignalName,
        ProductModule Module,
        decimal ImpactPercent,
        int OccurrenceCount,
        TrendDirection Trend,
        string Insight);
}
