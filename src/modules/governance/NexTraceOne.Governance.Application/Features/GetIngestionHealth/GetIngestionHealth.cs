using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetIngestionHealth;

/// <summary>
/// Feature: GetIngestionHealth — resumo de saúde do pipeline de ingestão.
/// Apresenta estado global, contadores por estado de conector, resumo de frescura por domínio e problemas críticos.
/// </summary>
public static class GetIngestionHealth
{
    /// <summary>Query para obter resumo de saúde da ingestão. Filtro opcional por conector.</summary>
    public sealed record Query(Guid? ConnectorId = null) : IQuery<Response>;

    /// <summary>Handler que retorna o resumo de saúde do pipeline de ingestão.</summary>
    public sealed class Handler(
        IIntegrationConnectorRepository connectorRepository,
        IIngestionSourceRepository sourceRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;

            // Get connector health counts
            var healthyCount = await connectorRepository.CountByHealthAsync(ConnectorHealth.Healthy, cancellationToken);
            var degradedCount = await connectorRepository.CountByHealthAsync(ConnectorHealth.Degraded, cancellationToken);
            var unhealthyCount = await connectorRepository.CountByHealthAsync(ConnectorHealth.Unhealthy, cancellationToken);
            var criticalCount = await connectorRepository.CountByHealthAsync(ConnectorHealth.Critical, cancellationToken);
            var failedCount = unhealthyCount + criticalCount;

            // Get source freshness counts
            var staleCount = await sourceRepository.CountByFreshnessStatusAsync(FreshnessStatus.Stale, cancellationToken);
            var outdatedCount = await sourceRepository.CountByFreshnessStatusAsync(FreshnessStatus.Outdated, cancellationToken);
            var expiredCount = await sourceRepository.CountByFreshnessStatusAsync(FreshnessStatus.Expired, cancellationToken);
            var staleFeeds = staleCount + outdatedCount + expiredCount;

            // Get all sources for freshness summary grouped by type
            var sources = await sourceRepository.ListAsync(
                connectorId: null,
                status: SourceStatus.Active,
                freshnessStatus: null,
                ct: cancellationToken);

            var freshnessSummary = sources
                .GroupBy(s => s.SourceType)
                .Select(g =>
                {
                    var latestSource = g.OrderByDescending(s => s.LastDataReceivedAt).FirstOrDefault();
                    var lagMinutes = latestSource?.LastDataReceivedAt.HasValue == true
                        ? (long)(now - latestSource.LastDataReceivedAt.Value).TotalMinutes
                        : 0;

                    var worstFreshness = g.Select(s => s.FreshnessStatus).Max();

                    return new DomainFreshness(
                        Domain: g.Key,
                        Status: worstFreshness.ToString(),
                        LastReceivedAt: latestSource?.LastDataReceivedAt,
                        LagMinutes: lagMinutes,
                        SourceCount: g.Count());
                })
                .ToList();

            // Get connectors with issues for critical issues list
            var connectors = await connectorRepository.ListAsync(
                status: null,
                health: null,
                connectorType: null,
                search: null,
                ct: cancellationToken);

            var criticalIssues = connectors
                .Where(c => c.Health is ConnectorHealth.Critical or ConnectorHealth.Unhealthy or ConnectorHealth.Degraded)
                .Select(c => $"{c.Name} connector {c.Health.ToString().ToLower()}" +
                    (c.LastErrorMessage is not null ? $" — {c.LastErrorMessage}" : ""))
                .ToList();

            // Determine overall status
            string overallStatus = (failedCount, degradedCount) switch
            {
                ( > 0, _) => "Critical",
                (_, > 0) => "Degraded",
                _ when staleFeeds > 0 => "Warning",
                _ => "Healthy"
            };

            var response = new Response(
                OverallStatus: overallStatus,
                HealthyConnectors: healthyCount,
                DegradedConnectors: degradedCount,
                FailedConnectors: failedCount,
                StaleFeeds: staleFeeds,
                FreshnessSummary: freshnessSummary,
                CriticalIssues: criticalIssues,
                LastCheckedAt: now);

            return Result<Response>.Success(response);
        }
    }

    /// <summary>Resposta com resumo de saúde do pipeline de ingestão.</summary>
    public sealed record Response(
        string OverallStatus,
        int HealthyConnectors,
        int DegradedConnectors,
        int FailedConnectors,
        int StaleFeeds,
        IReadOnlyList<DomainFreshness> FreshnessSummary,
        IReadOnlyList<string> CriticalIssues,
        DateTimeOffset LastCheckedAt);

    /// <summary>DTO de frescura por domínio de dados.</summary>
    public sealed record DomainFreshness(
        string Domain,
        string Status,
        DateTimeOffset? LastReceivedAt,
        long LagMinutes,
        int SourceCount);
}
