using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetServiceReliabilityDetail;

/// <summary>
/// Feature: GetServiceReliabilityDetail — visão consolidada real de confiabilidade de um serviço.
/// Compõe dados de RuntimeIntelligence, Incidents e Reliability snapshots históricos.
/// </summary>
public static class GetServiceReliabilityDetail
{
    public sealed record Query(string ServiceId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty().MaximumLength(200);
        }
    }

    public sealed class Handler(
        IReliabilityRuntimeSurface runtimeSurface,
        IReliabilityIncidentSurface incidentSurface,
        IReliabilitySnapshotRepository snapshotRepository,
        ICurrentTenant tenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var signal = await runtimeSurface.GetLatestSignalAsync(request.ServiceId, string.Empty, cancellationToken);
            var incidents = await incidentSurface.GetActiveIncidentsAsync(request.ServiceId, tenant.Id, cancellationToken);
            var history = await snapshotRepository.GetHistoryAsync(request.ServiceId, tenant.Id, 2, cancellationToken);

            if (signal is null && incidents.Count == 0)
            {
                Result<Response> notFound = Error.NotFound("Reliability.ServiceNotFound",
                    "Service '{0}' not found", request.ServiceId);
                return notFound;
            }

            var obsScore = await runtimeSurface.GetObservabilityScoreAsync(
                request.ServiceId, signal?.Environment ?? string.Empty, cancellationToken);

            var runtimeHealthScore = ComputeRuntimeHealthScore(signal?.HealthStatus);
            var incidentImpactScore = ComputeIncidentImpactScore(incidents);
            var observabilityScore = obsScore is not null ? obsScore.Value * 100m : 50m;
            var overallScore = Math.Round((runtimeHealthScore * 0.50m) + (incidentImpactScore * 0.30m) + (observabilityScore * 0.20m), 2);

            var trend = ComputeTrend(overallScore, history);
            var openIncidentCount = incidents.Count;
            var reliabilityStatus = DeriveReliabilityStatus(signal?.HealthStatus, overallScore, openIncidentCount);
            var flags = DeriveOperationalFlags(incidents, signal);
            var teamName = incidents.FirstOrDefault(i => !string.IsNullOrEmpty(i.TeamName))?.TeamName ?? string.Empty;

            var linkedIncidents = incidents.Select(i => new IncidentSummaryItem(
                Guid.Empty,
                string.Empty,
                $"{i.Severity} incident",
                i.Status,
                i.DetectedAt)).ToList();

            var coverage = new ReliabilityCoverageIndicators(
                HasOperationalSignals: signal is not null,
                HasRunbook: false,
                HasOwner: !string.IsNullOrEmpty(teamName),
                HasDependenciesMapped: false,
                HasRecentChangeContext: false,
                HasIncidentLinkage: openIncidentCount > 0);

            var metrics = signal is not null
                ? new OperationalMetrics(
                    AvailabilityPercent: signal.ErrorRate > 0 ? Math.Round((1m - signal.ErrorRate) * 100m, 2) : 100m,
                    LatencyP99Ms: signal.P99LatencyMs,
                    ErrorRatePercent: Math.Round(signal.ErrorRate * 100m, 2),
                    RequestsPerSecond: signal.RequestsPerSecond,
                    QueueLag: null,
                    ProcessingDelay: null)
                : new OperationalMetrics(0m, 0m, 0m, 0m, null, null);

            var anomalySummary = signal?.HealthStatus is "Degraded" or "Unhealthy"
                ? $"Health anomaly detected: service is {signal.HealthStatus}. Error rate: {signal.ErrorRate * 100m:F1}%, P99: {signal.P99LatencyMs:F0}ms."
                : "No anomalies detected.";

            var response = new Response(
                Identity: new ServiceIdentity(request.ServiceId, request.ServiceId, string.Empty, string.Empty, teamName, string.Empty),
                Status: reliabilityStatus,
                OperationalSummary: BuildOperationalSummary(reliabilityStatus, signal, openIncidentCount),
                Trend: new TrendSummary(trend, "30d", BuildTrendSummary(trend, history)),
                Metrics: metrics,
                ActiveFlags: flags,
                RecentChanges: [],
                LinkedIncidents: linkedIncidents,
                Dependencies: [],
                LinkedContracts: [],
                Runbooks: [],
                AnomalySummary: anomalySummary,
                Coverage: coverage);

            return Result<Response>.Success(response);
        }

        private static decimal ComputeRuntimeHealthScore(string? healthStatus) =>
            healthStatus switch
            {
                "Healthy" => 100m,
                "Degraded" => 60m,
                "Unhealthy" => 20m,
                _ => 50m
            };

        private static decimal ComputeIncidentImpactScore(IReadOnlyList<ReliabilityIncidentSignal> incidents)
        {
            if (incidents.Count == 0) return 100m;
            var critical = incidents.Count(i => i.Severity == IncidentSeverity.Critical.ToString());
            var major = incidents.Count(i => i.Severity == IncidentSeverity.Major.ToString());
            var minor = incidents.Count(i => i.Severity == IncidentSeverity.Minor.ToString());
            var warning = incidents.Count(i => i.Severity == IncidentSeverity.Warning.ToString());
            return Math.Max(0m, 100m - (critical * 30m + major * 20m + minor * 10m + warning * 5m));
        }

        private static TrendDirection ComputeTrend(decimal currentScore, IReadOnlyList<Domain.Reliability.Entities.ReliabilitySnapshot> history)
        {
            if (history.Count < 2) return TrendDirection.Stable;
            var previous = history[1].OverallScore;
            if (currentScore > previous + 5m) return TrendDirection.Improving;
            if (currentScore < previous - 5m) return TrendDirection.Declining;
            return TrendDirection.Stable;
        }

        private static string BuildTrendSummary(TrendDirection trend, IReadOnlyList<Domain.Reliability.Entities.ReliabilitySnapshot> history) =>
            trend switch
            {
                TrendDirection.Improving => "Reliability score improved over the last period.",
                TrendDirection.Declining => "Reliability score declined over the last period.",
                _ => history.Count == 0 ? "Insufficient history to determine trend." : "Score stable over the last period."
            };

        private static ReliabilityStatus DeriveReliabilityStatus(string? healthStatus, decimal overallScore, int openIncidents)
        {
            if (healthStatus == "Unhealthy" || openIncidents > 0 && overallScore < 40m)
                return ReliabilityStatus.Unavailable;
            if (healthStatus == "Degraded" || overallScore < 60m)
                return ReliabilityStatus.Degraded;
            if (overallScore < 75m || openIncidents > 0)
                return ReliabilityStatus.NeedsAttention;
            return ReliabilityStatus.Healthy;
        }

        private static OperationalFlag DeriveOperationalFlags(
            IReadOnlyList<ReliabilityIncidentSignal> incidents, RuntimeServiceSignal? signal)
        {
            var flags = OperationalFlag.None;
            if (incidents.Count > 0) flags |= OperationalFlag.IncidentLinked;
            if (signal?.HealthStatus is "Degraded" or "Unhealthy") flags |= OperationalFlag.AnomalyDetected;
            return flags;
        }

        private static string BuildOperationalSummary(
            ReliabilityStatus status, RuntimeServiceSignal? signal, int openIncidents) =>
            status switch
            {
                ReliabilityStatus.Healthy => "Service operating within expected parameters. All health checks passing.",
                ReliabilityStatus.Degraded => $"Degraded performance detected. Error rate: {signal?.ErrorRate * 100m:F1}%. P99 latency: {signal?.P99LatencyMs:F0}ms.",
                ReliabilityStatus.Unavailable => $"Service unavailable or severely impaired. {openIncidents} active incident(s).",
                ReliabilityStatus.NeedsAttention => $"{openIncidents} open incident(s) require attention.",
                _ => string.Empty
            };
    }

    public sealed record ServiceIdentity(
        string ServiceId, string DisplayName, string ServiceType,
        string Domain, string TeamName, string Criticality);

    public sealed record TrendSummary(
        TrendDirection Direction, string Timeframe, string Summary);

    public sealed record OperationalMetrics(
        decimal AvailabilityPercent, decimal LatencyP99Ms, decimal ErrorRatePercent,
        decimal RequestsPerSecond, decimal? QueueLag, decimal? ProcessingDelay);

    public sealed record ChangeSummaryItem(
        Guid ChangeId, string Description, string ChangeType,
        string ConfidenceStatus, DateTimeOffset DeployedAt);

    public sealed record IncidentSummaryItem(
        Guid IncidentId, string Reference, string Title,
        string Status, DateTimeOffset ReportedAt);

    public sealed record DependencySummaryItem(
        string ServiceId, string DisplayName, ReliabilityStatus Status);

    public sealed record ContractSummaryItem(
        Guid ContractVersionId, string Name, string Version,
        string Protocol, string LifecycleState);

    public sealed record RunbookSummaryItem(string Title, string? Url);

    public sealed record ReliabilityCoverageIndicators(
        bool HasOperationalSignals, bool HasRunbook, bool HasOwner,
        bool HasDependenciesMapped, bool HasRecentChangeContext,
        bool HasIncidentLinkage);

    public sealed record Response(
        ServiceIdentity Identity,
        ReliabilityStatus Status,
        string OperationalSummary,
        TrendSummary Trend,
        OperationalMetrics Metrics,
        OperationalFlag ActiveFlags,
        IReadOnlyList<ChangeSummaryItem> RecentChanges,
        IReadOnlyList<IncidentSummaryItem> LinkedIncidents,
        IReadOnlyList<DependencySummaryItem> Dependencies,
        IReadOnlyList<ContractSummaryItem> LinkedContracts,
        IReadOnlyList<RunbookSummaryItem> Runbooks,
        string AnomalySummary,
        ReliabilityCoverageIndicators Coverage);
}
