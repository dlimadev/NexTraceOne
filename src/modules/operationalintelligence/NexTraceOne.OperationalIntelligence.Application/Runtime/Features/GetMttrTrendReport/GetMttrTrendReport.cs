using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetMttrTrendReport;

/// <summary>
/// Feature: GetMttrTrendReport — tendência de MTTR (Mean Time to Restore) por serviço.
///
/// Deriva o MTTR a partir das observações de SLO: um "incidente" começa quando uma
/// observação de SLO entra em estado Breached e termina quando a observação seguinte
/// do mesmo serviço/ambiente volta ao estado Met. O MTTR é calculado como a média das
/// durações dessas transições Breached→Met no período.
///
/// Classifica cada serviço por DORA MTTR tier:
/// - <c>Elite</c>  — MTTR médio &lt; 1 hora
/// - <c>High</c>   — MTTR médio 1–4 horas
/// - <c>Medium</c> — MTTR médio 4–24 horas
/// - <c>Low</c>    — MTTR médio &gt; 24 horas
/// - <c>Insufficient</c> — sem dados de breach/restore suficientes no período
///
/// Classifica a tendência de cada serviço (comparando primeira vs. segunda metade do período):
/// - <c>Improving</c>  — MTTR da segunda metade &lt; MTTR da primeira metade (melhoria)
/// - <c>Worsening</c>  — MTTR da segunda metade &gt; MTTR da primeira metade (degradação)
/// - <c>Stable</c>     — diferença inferior a 10%
/// - <c>Insufficient</c> — menos de 2 eventos Breached→Met para calcular tendência
///
/// Produz:
/// - série temporal diária de MTTR médio agregado do tenant (últimos 30 dias)
/// - lista de serviços classificados por tier e tendência
/// - top serviços com maior MTTR e tendência de pioria
///
/// Wave S.3 — MTTR Trend Report (OperationalIntelligence Runtime).
/// </summary>
public static class GetMttrTrendReport
{
    // ── DORA MTTR thresholds (em horas) ──────────────────────────────────
    private const double EliteThresholdHours = 1.0;
    private const double HighThresholdHours = 4.0;
    private const double MediumThresholdHours = 24.0;

    // ── Trend thresholds ──────────────────────────────────────────────────
    private const double TrendImprovingThresholdPct = -0.10; // -10%
    private const double TrendWorseningThresholdPct = 0.10;  // +10%

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: janela temporal em dias (1–90, default 30).</para>
    /// <para><c>TopWorstCount</c>: número máximo de serviços com pior MTTR a listar (1–100, default 10).</para>
    /// <para><c>Environment</c>: filtro opcional de ambiente (null = todos).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = 30,
        int TopWorstCount = 10,
        string? Environment = null) : IQuery<Report>;

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Classificação DORA de MTTR de um serviço.</summary>
    public enum MttrDoraLevel
    {
        /// <summary>MTTR médio &lt; 1 hora — elite performer.</summary>
        Elite,
        /// <summary>MTTR médio 1–4 horas — alto desempenho.</summary>
        High,
        /// <summary>MTTR médio 4–24 horas — desempenho médio.</summary>
        Medium,
        /// <summary>MTTR médio &gt; 24 horas — baixo desempenho.</summary>
        Low,
        /// <summary>Dados insuficientes para classificar.</summary>
        Insufficient
    }

    /// <summary>Tendência de evolução do MTTR de um serviço.</summary>
    public enum MttrTrend
    {
        /// <summary>MTTR está a melhorar (reduzindo) na segunda metade do período.</summary>
        Improving,
        /// <summary>MTTR está estável (&lt;10% de variação).</summary>
        Stable,
        /// <summary>MTTR está a piorar (aumentando) na segunda metade do período.</summary>
        Worsening,
        /// <summary>Dados insuficientes para calcular tendência.</summary>
        Insufficient
    }

    /// <summary>Distribuição de serviços por tier DORA de MTTR.</summary>
    public sealed record DoraLevelDistribution(
        int EliteCount,
        int HighCount,
        int MediumCount,
        int LowCount,
        int InsufficientCount);

    /// <summary>Ponto de série temporal com MTTR médio diário.</summary>
    public sealed record MttrDailyDataPoint(
        DateOnly Date,
        double AvgMttrHours,
        int BreachEventCount);

    /// <summary>Detalhe de MTTR por serviço.</summary>
    public sealed record ServiceMttrEntry(
        string ServiceName,
        string Environment,
        int TotalBreachEvents,
        double AvgMttrHours,
        double MaxMttrHours,
        MttrDoraLevel DoraLevel,
        MttrTrend Trend);

    /// <summary>Resultado do relatório de tendência de MTTR.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        int LookbackDays,
        int TotalServicesAnalyzed,
        int TotalBreachEvents,
        double TenantAvgMttrHours,
        DoraLevelDistribution LevelDistribution,
        IReadOnlyList<MttrDailyDataPoint> DailyMttrSeries,
        IReadOnlyList<ServiceMttrEntry> TopWorstMttrServices,
        IReadOnlyList<ServiceMttrEntry> AllServices);

    // ── Validator ──────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.LookbackDays).InclusiveBetween(1, 90);
            RuleFor(q => q.TopWorstCount).InclusiveBetween(1, 100);
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly ISloObservationRepository _sloRepo;
        private readonly IDateTimeProvider _clock;

        public Handler(
            ISloObservationRepository sloRepo,
            IDateTimeProvider clock)
        {
            _sloRepo = Guard.Against.Null(sloRepo);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var now = _clock.UtcNow;
            var from = now.AddDays(-query.LookbackDays);

            var observations = await _sloRepo.ListByTenantAsync(
                query.TenantId,
                since: from,
                until: now,
                statusFilter: null,
                ct: cancellationToken);

            // Filter by environment if specified
            var filtered = query.Environment is null
                ? observations
                : observations
                    .Where(o => string.Equals(o.Environment, query.Environment, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            if (filtered.Count == 0)
            {
                return Result<Report>.Success(new Report(
                    GeneratedAt: now,
                    LookbackDays: query.LookbackDays,
                    TotalServicesAnalyzed: 0,
                    TotalBreachEvents: 0,
                    TenantAvgMttrHours: 0,
                    LevelDistribution: new DoraLevelDistribution(0, 0, 0, 0, 0),
                    DailyMttrSeries: [],
                    TopWorstMttrServices: [],
                    AllServices: []));
            }

            var midpoint = from.AddSeconds((now - from).TotalSeconds / 2);

            // Group by service+environment, then compute MTTR from Breached→Met transitions
            var serviceGroups = filtered
                .GroupBy(o => (o.ServiceName, o.Environment))
                .ToList();

            var entries = new List<ServiceMttrEntry>();
            var allBreachDurationsHours = new List<(DateTimeOffset BreachStart, double DurationHours)>();

            foreach (var group in serviceGroups)
            {
                var sortedObs = group.OrderBy(o => o.ObservedAt).ToList();

            // Compute Breached→Met transitions
            var breachEvents = new List<(DateTimeOffset Start, DateTimeOffset End)>();
            DateTimeOffset? breachStart = null;

            foreach (var obs in sortedObs)
            {
                if (obs.Status == SloObservationStatus.Breached && breachStart is null)
                {
                    breachStart = obs.ObservedAt;
                }
                else if (obs.Status == SloObservationStatus.Met && breachStart is not null)
                {
                    breachEvents.Add((breachStart.Value, obs.ObservedAt));
                    breachStart = null;
                }
            }

            if (breachEvents.Count == 0)
                continue;

            var durationsHours = breachEvents
                .Select(e => (e.End - e.Start).TotalHours)
                .ToList();

            double avgMttr = durationsHours.Average();
            double maxMttr = durationsHours.Max();

            // Trend: compare first half vs second half of period
            var firstHalf = breachEvents.Where(e => e.Start < midpoint).ToList();
            var secondHalf = breachEvents.Where(e => e.Start >= midpoint).ToList();

            MttrTrend trend;
            if (firstHalf.Count < 1 || secondHalf.Count < 1)
            {
                trend = MttrTrend.Insufficient;
            }
            else
            {
                double firstAvg = firstHalf.Average(e => (e.End - e.Start).TotalHours);
                double secondAvg = secondHalf.Average(e => (e.End - e.Start).TotalHours);
                double delta = firstAvg > 0 ? (secondAvg - firstAvg) / firstAvg : 0;
                trend = delta <= TrendImprovingThresholdPct ? MttrTrend.Improving
                      : delta >= TrendWorseningThresholdPct ? MttrTrend.Worsening
                      : MttrTrend.Stable;
            }

            entries.Add(new ServiceMttrEntry(
                ServiceName: group.Key.ServiceName,
                Environment: group.Key.Environment,
                TotalBreachEvents: breachEvents.Count,
                AvgMttrHours: Math.Round(avgMttr, 2),
                MaxMttrHours: Math.Round(maxMttr, 2),
                DoraLevel: ClassifyDora(avgMttr),
                Trend: trend));

            foreach (var (start, end) in breachEvents)
                allBreachDurationsHours.Add((start, (end - start).TotalHours));
            }

            // Daily MTTR series: average of breach durations per day
            var dailySeries = BuildDailySeries(allBreachDurationsHours, from, now);

            // Distribution
            int eliteCount = entries.Count(e => e.DoraLevel == MttrDoraLevel.Elite);
            int highCount = entries.Count(e => e.DoraLevel == MttrDoraLevel.High);
            int mediumCount = entries.Count(e => e.DoraLevel == MttrDoraLevel.Medium);
            int lowCount = entries.Count(e => e.DoraLevel == MttrDoraLevel.Low);

            // Services with no breach events are excluded from entries; count them as insufficient
            int allServiceNames = filtered.Select(o => (o.ServiceName, o.Environment)).Distinct().Count();
            int insufficientCount = allServiceNames - entries.Count;

            double tenantAvg = entries.Count > 0
                ? Math.Round(entries.Average(e => e.AvgMttrHours), 2)
                : 0;

            var topWorst = entries
                .OrderByDescending(e => e.AvgMttrHours)
                .Take(query.TopWorstCount)
                .ToList();

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                LookbackDays: query.LookbackDays,
                TotalServicesAnalyzed: allServiceNames,
                TotalBreachEvents: entries.Sum(e => e.TotalBreachEvents),
                TenantAvgMttrHours: tenantAvg,
                LevelDistribution: new DoraLevelDistribution(eliteCount, highCount, mediumCount, lowCount, insufficientCount < 0 ? 0 : insufficientCount),
                DailyMttrSeries: dailySeries,
                TopWorstMttrServices: topWorst,
                AllServices: entries.OrderBy(e => e.ServiceName).ThenBy(e => e.Environment).ToList()));
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static MttrDoraLevel ClassifyDora(double avgHours) => avgHours switch
        {
            < EliteThresholdHours => MttrDoraLevel.Elite,
            < HighThresholdHours => MttrDoraLevel.High,
            < MediumThresholdHours => MttrDoraLevel.Medium,
            _ => MttrDoraLevel.Low
        };

        private static IReadOnlyList<MttrDailyDataPoint> BuildDailySeries(
            IReadOnlyList<(DateTimeOffset BreachStart, double DurationHours)> events,
            DateTimeOffset from,
            DateTimeOffset until)
        {
            var result = new List<MttrDailyDataPoint>();
            var current = from.Date;
            var end = until.Date;

            while (current <= end)
            {
                var dayEvents = events
                    .Where(e => DateOnly.FromDateTime(e.BreachStart.Date) == DateOnly.FromDateTime(current))
                    .ToList();

                result.Add(new MttrDailyDataPoint(
                    Date: DateOnly.FromDateTime(current),
                    AvgMttrHours: dayEvents.Count > 0 ? Math.Round(dayEvents.Average(e => e.DurationHours), 2) : 0,
                    BreachEventCount: dayEvents.Count));

                current = current.AddDays(1);
            }

            return result;
        }
    }
}
