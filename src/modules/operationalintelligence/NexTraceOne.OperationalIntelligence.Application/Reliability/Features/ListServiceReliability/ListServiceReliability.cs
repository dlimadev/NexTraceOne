using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.ListServiceReliability;

/// <summary>
/// Feature: ListServiceReliability — lista serviços com resumo de confiabilidade.
/// Compõe dados reais de RuntimeIntelligence e Incidents para calcular scores de confiabilidade
/// por serviço. Retorna paginação com OverallScore, TrendDirection e estado operacional.
/// </summary>
public static class ListServiceReliability
{
    public sealed record Query(
        string? TeamId,
        string? ServiceId,
        string? Domain,
        string? Environment,
        ReliabilityStatus? Status,
        string? ServiceType,
        string? Criticality,
        string? Search,
        int Page,
        int PageSize) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.TeamId).MaximumLength(200).When(x => x.TeamId is not null);
            RuleFor(x => x.ServiceId).MaximumLength(200).When(x => x.ServiceId is not null);
            RuleFor(x => x.Domain).MaximumLength(200).When(x => x.Domain is not null);
            RuleFor(x => x.Search).MaximumLength(200).When(x => x.Search is not null);
        }
    }

    public sealed class Handler(
        IReliabilityRuntimeSurface runtimeSurface,
        IReliabilityIncidentSurface incidentSurface,
        ICurrentTenant tenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var runtimeSignals = await runtimeSurface
                .GetLatestSignalsAllServicesAsync(request.Environment, cancellationToken);

            var incidentSignals = await incidentSurface
                .GetAllServicesIncidentSignalsAsync(tenant.Id, cancellationToken);

            var observabilityScores = await runtimeSurface
                .GetObservabilityScoresAllServicesAsync(request.Environment, cancellationToken);

            // União de nomes de serviços de ambas as fontes.
            var serviceNames = runtimeSignals.Select(r => r.ServiceName)
                .Union(incidentSignals.Select(i => i.ServiceId))
                .Distinct()
                .ToList();

            var runtimeByService = runtimeSignals.ToDictionary(r => r.ServiceName);
            var incidentsByService = incidentSignals
                .GroupBy(i => i.ServiceId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var now = DateTimeOffset.UtcNow;
            var items = new List<ServiceReliabilityItem>();

            foreach (var serviceName in serviceNames)
            {
                runtimeByService.TryGetValue(serviceName, out var signal);
                incidentsByService.TryGetValue(serviceName, out var incidents);
                observabilityScores.TryGetValue(serviceName, out var obsScore);

                var runtimeHealthScore = ComputeRuntimeHealthScore(signal?.HealthStatus);
                var incidentImpactScore = ComputeIncidentImpactScore(incidents);
                var observabilityScore = obsScore > 0 ? obsScore * 100m : 50m;
                var overallScore = (runtimeHealthScore * 0.50m) + (incidentImpactScore * 0.30m) + (observabilityScore * 0.20m);

                var openIncidentCount = incidents?.Count ?? 0;
                var reliabilityStatus = DeriveReliabilityStatus(signal?.HealthStatus, overallScore, openIncidentCount);
                var operationalFlags = DeriveOperationalFlags(incidents, signal);
                var teamName = incidents?.FirstOrDefault(i => !string.IsNullOrEmpty(i.TeamName))?.TeamName ?? string.Empty;

                var item = new ServiceReliabilityItem(
                    ServiceName: serviceName,
                    DisplayName: serviceName,
                    ServiceType: string.Empty,
                    Domain: string.Empty,
                    TeamName: teamName,
                    Criticality: string.Empty,
                    ReliabilityStatus: reliabilityStatus,
                    OperationalSummary: BuildOperationalSummary(reliabilityStatus, signal, openIncidentCount),
                    Trend: TrendDirection.Stable,
                    ActiveFlags: operationalFlags,
                    OpenIncidents: openIncidentCount,
                    RecentChangeImpact: operationalFlags.HasFlag(OperationalFlag.RecentChangeImpact),
                    OverallScore: Math.Round(overallScore, 2),
                    LastComputedAt: now);

                items.Add(item);
            }

            var filtered = items.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(request.TeamId))
                filtered = filtered.Where(i => i.TeamName.Equals(request.TeamId, StringComparison.OrdinalIgnoreCase));

            if (request.Status.HasValue)
                filtered = filtered.Where(i => i.ReliabilityStatus == request.Status.Value);

            if (!string.IsNullOrWhiteSpace(request.Domain))
                filtered = filtered.Where(i => i.Domain.Equals(request.Domain, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(request.ServiceType))
                filtered = filtered.Where(i => i.ServiceType.Equals(request.ServiceType, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(request.Criticality))
                filtered = filtered.Where(i => i.Criticality.Equals(request.Criticality, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(request.Search))
                filtered = filtered.Where(i =>
                    i.ServiceName.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                    i.DisplayName.Contains(request.Search, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(request.ServiceId))
                filtered = filtered.Where(i => i.ServiceName.Equals(request.ServiceId, StringComparison.OrdinalIgnoreCase));

            var filteredList = filtered.ToList();
            var totalCount = filteredList.Count;
            var paged = filteredList.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();

            return Result<Response>.Success(new Response(paged, totalCount, request.Page, request.PageSize));
        }

        private static decimal ComputeRuntimeHealthScore(string? healthStatus) =>
            healthStatus switch
            {
                "Healthy" => 100m,
                "Degraded" => 60m,
                "Unhealthy" => 20m,
                _ => 50m // Unknown or not found
            };

        private static decimal ComputeIncidentImpactScore(List<ReliabilityIncidentSignal>? incidents)
        {
            if (incidents is null or { Count: 0 }) return 100m;
            var critical = incidents.Count(i => i.Severity == IncidentSeverity.Critical.ToString());
            var major = incidents.Count(i => i.Severity == IncidentSeverity.Major.ToString());
            var minor = incidents.Count(i => i.Severity == IncidentSeverity.Minor.ToString());
            var warning = incidents.Count(i => i.Severity == IncidentSeverity.Warning.ToString());
            return Math.Max(0m, 100m - (critical * 30m + major * 20m + minor * 10m + warning * 5m));
        }

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
            List<ReliabilityIncidentSignal>? incidents, RuntimeServiceSignal? signal)
        {
            var flags = OperationalFlag.None;
            if (incidents is { Count: > 0 }) flags |= OperationalFlag.IncidentLinked;
            if (signal?.HealthStatus is "Degraded" or "Unhealthy") flags |= OperationalFlag.AnomalyDetected;
            return flags;
        }

        private static string BuildOperationalSummary(
            ReliabilityStatus status, RuntimeServiceSignal? signal, int openIncidents) =>
            status switch
            {
                ReliabilityStatus.Healthy => "Service operating within expected parameters.",
                ReliabilityStatus.Degraded => $"Degraded performance detected. Error rate: {signal?.ErrorRate * 100m:F1}%. P99: {signal?.P99LatencyMs:F0}ms.",
                ReliabilityStatus.Unavailable => $"Service unavailable or severely impaired. {openIncidents} active incident(s).",
                ReliabilityStatus.NeedsAttention => $"{openIncidents} open incident(s) require attention.",
                _ => string.Empty
            };
    }

    public sealed record ServiceReliabilityItem(
        string ServiceName,
        string DisplayName,
        string ServiceType,
        string Domain,
        string TeamName,
        string Criticality,
        ReliabilityStatus ReliabilityStatus,
        string OperationalSummary,
        TrendDirection Trend,
        OperationalFlag ActiveFlags,
        int OpenIncidents,
        bool RecentChangeImpact,
        decimal OverallScore,
        DateTimeOffset LastComputedAt);

    public sealed record Response(
        IReadOnlyList<ServiceReliabilityItem> Items,
        int TotalCount,
        int Page,
        int PageSize);
}
