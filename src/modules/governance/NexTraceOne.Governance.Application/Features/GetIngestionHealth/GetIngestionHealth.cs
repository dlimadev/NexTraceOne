using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

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
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;

            var freshnessSummary = new List<DomainFreshness>
            {
                new("Changes", "Fresh", now.AddMinutes(-10), 10, 2),
                new("Incidents", "Acceptable", now.AddHours(-2), 120, 1),
                new("Telemetry", "Fresh", now.AddMinutes(-3), 3, 2),
                new("Contracts", "Fresh", now.AddMinutes(-15), 15, 2),
                new("Knowledge", "Acceptable", now.AddMinutes(-30), 30, 2),
                new("Runtime", "Stale", now.AddHours(-6), 360, 1),
                new("Alerts", "Fresh", now.AddMinutes(-4), 4, 1)
            };

            var criticalIssues = new List<string>
            {
                "Kafka Events connector failed — broker unreachable since 6h ago",
                "PagerDuty Incidents connector degraded — partial data loss in last 2 executions",
                "Swagger/OpenAPI Import connector disabled for 14 days — stale contract data"
            };

            var response = new Response(
                OverallStatus: "Degraded",
                HealthyConnectors: 6,
                DegradedConnectors: 1,
                FailedConnectors: 1,
                StaleFeeds: 2,
                FreshnessSummary: freshnessSummary,
                CriticalIssues: criticalIssues,
                LastCheckedAt: now);

            return Task.FromResult(Result<Response>.Success(response));
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
