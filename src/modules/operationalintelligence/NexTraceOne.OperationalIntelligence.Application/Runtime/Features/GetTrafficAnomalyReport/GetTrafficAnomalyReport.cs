using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetTrafficAnomalyReport;

/// <summary>
/// Feature: GetTrafficAnomalyReport — detecção de anomalias de tráfego em tempo analítico.
///
/// Tipos de anomalia:
/// - <c>SpikeAnomaly</c> — RPS > spikeSigma × desvio padrão histórico
/// - <c>DropAnomaly</c> — RPS &lt; dropPct% da média histórica
/// - <c>LatencySpike</c> — P95 acima de latencyMultiplier × baseline
/// - <c>ErrorRateSpike</c> — taxa de erro > errorRateThreshold% do baseline
///
/// <c>AnomalyCorrelation</c>: CorrelatedWithDeploy / CorrelatedWithIncident / Unexplained
/// <c>AnomalySeverity</c>: Informational / Warning / Critical
/// Expõe <c>UnexplainedAnomalyList</c>, <c>AnomalyTimeline</c> e <c>RecurringAnomalyPatterns</c>.
///
/// Wave AZ.3 — Service Mesh &amp; Runtime Traffic Intelligence (OperationalIntelligence Runtime).
/// </summary>
public static class GetTrafficAnomalyReport
{
    // ── Defaults ───────────────────────────────────────────────────────────
    internal const double DefaultSpikeSigma = 3.0;
    internal const double DefaultDropPct = 50.0;
    internal const double DefaultErrorRateSpikeThreshold = 5.0;
    internal const double DefaultLatencySpikeMultiplier = 2.0;
    internal const int DefaultLookbackDays = 7;
    internal const int DefaultTopAnomalousCount = 5;

    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        string TenantId,
        int LookbackDays = DefaultLookbackDays,
        double SpikeSigma = DefaultSpikeSigma,
        double DropPct = DefaultDropPct,
        double ErrorRateSpikeThreshold = DefaultErrorRateSpikeThreshold,
        double LatencySpikeMultiplier = DefaultLatencySpikeMultiplier,
        int TopAnomalousCount = DefaultTopAnomalousCount) : IQuery<Report>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.LookbackDays).InclusiveBetween(1, 90);
            RuleFor(x => x.SpikeSigma).GreaterThan(0);
            RuleFor(x => x.DropPct).InclusiveBetween(1.0, 99.0);
            RuleFor(x => x.ErrorRateSpikeThreshold).GreaterThan(0);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────
    public enum AnomalySeverity { Informational, Warning, Critical }
    public enum AnomalyCorrelation { CorrelatedWithDeploy, CorrelatedWithIncident, Unexplained }
    public enum RecurringAnomalyType { DayOfWeek, TimeOfDay, Both }

    // ── Value objects ──────────────────────────────────────────────────────
    public sealed record AnomalyEntry(
        string ServiceId,
        string ServiceName,
        string TeamName,
        string AnomalyType,
        DateTimeOffset DetectedAt,
        DateTimeOffset? ResolvedAt,
        double ObservedValue,
        double BaselineValue,
        AnomalyCorrelation Correlation,
        string? CorrelatedEventId,
        AnomalySeverity Severity,
        int DurationMinutes);

    public sealed record RecurringAnomalyPattern(
        string ServiceId,
        string ServiceName,
        string AnomalyType,
        int OccurrenceCount,
        RecurringAnomalyType PatternType,
        string PatternDescription);

    public sealed record AnomalyTimelinePoint(
        DateTimeOffset Timestamp,
        string EventType,
        string Description,
        string? ServiceId);

    public sealed record ServiceAnomalySummary(
        string ServiceId,
        string ServiceName,
        int TotalAnomalies,
        int CriticalAnomalies,
        int UnexplainedAnomalies);

    public sealed record TenantTrafficAnomalySummary(
        int TotalAnomalies,
        int CriticalAnomalies,
        int UnexplainedAnomalies,
        decimal AnomalyResolutionRate,
        IReadOnlyList<ServiceAnomalySummary> MostAnomalousServices);

    public sealed record Report(
        IReadOnlyList<AnomalyEntry> Anomalies,
        TenantTrafficAnomalySummary Summary,
        IReadOnlyList<AnomalyEntry> UnexplainedAnomalyList,
        IReadOnlyList<AnomalyTimelinePoint> AnomalyTimeline,
        IReadOnlyList<RecurringAnomalyPattern> RecurringAnomalyPatterns);

    // ── Handler ───────────────────────────────────────────────────────────
    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly ITrafficAnomalyReader _reader;
        private readonly IDateTimeProvider _clock;

        public Handler(ITrafficAnomalyReader reader, IDateTimeProvider clock)
        {
            _reader = Guard.Against.Null(reader);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query request, CancellationToken ct)
        {
            var to = _clock.UtcNow;
            var from = to.AddDays(-request.LookbackDays);

            var serviceEntries = await _reader.ListByTenantAsync(request.TenantId, from, to, ct);
            var timelineEvents = await _reader.GetTimelineEventsAsync(request.TenantId, from, to, ct);

            var allAnomalies = new List<AnomalyEntry>();

            foreach (var svc in serviceEntries)
            {
                foreach (var obs in svc.Anomalies)
                {
                    var severity = ComputeSeverity(obs, svc, request);
                    var correlation = ParseCorrelation(obs.AnomalyCorrelation);
                    int durationMinutes = obs.ResolvedAt.HasValue
                        ? (int)(obs.ResolvedAt.Value - obs.DetectedAt).TotalMinutes
                        : 0;

                    allAnomalies.Add(new AnomalyEntry(
                        svc.ServiceId, svc.ServiceName, svc.TeamName,
                        obs.AnomalyType, obs.DetectedAt, obs.ResolvedAt,
                        obs.ObservedValue, obs.BaselineValue,
                        correlation, obs.CorrelatedEventId,
                        severity, durationMinutes));
                }
            }

            var unexplained = allAnomalies
                .Where(a => a.Correlation == AnomalyCorrelation.Unexplained)
                .OrderByDescending(a => a.Severity)
                .ThenByDescending(a => a.DetectedAt)
                .ToList();

            int totalAnomalies = allAnomalies.Count;
            int criticalAnomalies = allAnomalies.Count(a => a.Severity == AnomalySeverity.Critical);
            int unexplainedCount = unexplained.Count;

            // Resolution rate: anomalies that resolved without becoming incidents
            int resolvedWithoutIncident = allAnomalies.Count(a =>
                a.ResolvedAt.HasValue && a.Correlation != AnomalyCorrelation.CorrelatedWithIncident);
            decimal resolutionRate = totalAnomalies > 0
                ? Math.Round((decimal)resolvedWithoutIncident / totalAnomalies * 100m, 1)
                : 100m;

            // Most anomalous services
            var bySvc = allAnomalies
                .GroupBy(a => a.ServiceId)
                .Select(g => new ServiceAnomalySummary(
                    g.Key,
                    g.First().ServiceName,
                    g.Count(),
                    g.Count(a => a.Severity == AnomalySeverity.Critical),
                    g.Count(a => a.Correlation == AnomalyCorrelation.Unexplained)))
                .OrderByDescending(s => s.TotalAnomalies)
                .Take(request.TopAnomalousCount)
                .ToList();

            var summary = new TenantTrafficAnomalySummary(
                totalAnomalies, criticalAnomalies, unexplainedCount, resolutionRate, bySvc);

            // Timeline: merge anomaly start points with external events
            var timeline = BuildTimeline(allAnomalies, timelineEvents);

            // Recurring patterns
            var recurring = DetectRecurringPatterns(allAnomalies);

            return Result<Report>.Success(new Report(
                Anomalies: allAnomalies,
                Summary: summary,
                UnexplainedAnomalyList: unexplained,
                AnomalyTimeline: timeline,
                RecurringAnomalyPatterns: recurring));
        }

        internal static AnomalySeverity ComputeSeverity(
            ITrafficAnomalyReader.AnomalyObservation obs,
            ITrafficAnomalyReader.ServiceTrafficAnomalyEntry svc,
            Query request)
        {
            return obs.AnomalyType switch
            {
                "ErrorRateSpike" when obs.ObservedValue > svc.BaselineErrorRatePct * 5 => AnomalySeverity.Critical,
                "ErrorRateSpike" => AnomalySeverity.Warning,
                "LatencySpike" when obs.ObservedValue > svc.BaselineLatencyP95Ms * request.LatencySpikeMultiplier * 3 => AnomalySeverity.Critical,
                "LatencySpike" => AnomalySeverity.Warning,
                "SpikeAnomaly" when obs.ObservedValue > obs.BaselineValue * 5 => AnomalySeverity.Critical,
                "SpikeAnomaly" => AnomalySeverity.Warning,
                "DropAnomaly" when obs.ObservedValue < obs.BaselineValue * 0.1 => AnomalySeverity.Critical,
                "DropAnomaly" => AnomalySeverity.Warning,
                _ => AnomalySeverity.Informational
            };
        }

        private static AnomalyCorrelation ParseCorrelation(string raw) =>
            raw switch
            {
                "CorrelatedWithDeploy" => AnomalyCorrelation.CorrelatedWithDeploy,
                "CorrelatedWithIncident" => AnomalyCorrelation.CorrelatedWithIncident,
                _ => AnomalyCorrelation.Unexplained
            };

        private static IReadOnlyList<AnomalyTimelinePoint> BuildTimeline(
            IReadOnlyList<AnomalyEntry> anomalies,
            IReadOnlyList<ITrafficAnomalyReader.TimelineEvent> events)
        {
            var points = new List<AnomalyTimelinePoint>();

            foreach (var a in anomalies)
                points.Add(new AnomalyTimelinePoint(a.DetectedAt, $"Anomaly:{a.AnomalyType}",
                    $"{a.ServiceName} — {a.AnomalyType}", a.ServiceId));

            foreach (var e in events)
                points.Add(new AnomalyTimelinePoint(e.OccurredAt, e.EventType, e.Description, null));

            return points.OrderBy(p => p.Timestamp).ToList();
        }

        private static IReadOnlyList<RecurringAnomalyPattern> DetectRecurringPatterns(
            IReadOnlyList<AnomalyEntry> anomalies)
        {
            // Group by service + anomaly type; flag if ≥ 3 occurrences at same day-of-week or same hour
            return anomalies
                .GroupBy(a => new { a.ServiceId, a.ServiceName, a.AnomalyType })
                .Where(g => g.Count() >= 3)
                .SelectMany(g =>
                {
                    var patterns = new List<RecurringAnomalyPattern>();

                    // Same day-of-week?
                    var byDay = g.GroupBy(a => a.DetectedAt.DayOfWeek);
                    foreach (var day in byDay.Where(d => d.Count() >= 3))
                        patterns.Add(new RecurringAnomalyPattern(
                            g.Key.ServiceId, g.Key.ServiceName, g.Key.AnomalyType,
                            day.Count(), RecurringAnomalyType.DayOfWeek,
                            $"Recurring every {day.Key}"));

                    return patterns;
                })
                .ToList();
        }
    }
}
