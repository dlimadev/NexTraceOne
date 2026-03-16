using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.ListIntegrationConnectors;

/// <summary>
/// Feature: ListIntegrationConnectors — lista conectores de integração registados no Integration Hub.
/// Permite filtragem por tipo, status, ambiente e pesquisa textual com paginação.
/// </summary>
public static class ListIntegrationConnectors
{
    /// <summary>Query para listar conectores com filtros e paginação.</summary>
    public sealed record Query(
        string? ConnectorType = null,
        string? Status = null,
        string? Environment = null,
        string? Search = null,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Handler que retorna a lista paginada de conectores de integração.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var connectors = new List<ConnectorListItem>
            {
                new(Guid.Parse("a1b2c3d4-0001-4000-8000-000000000001"),
                    "github-cicd", "GitHub CI/CD", "CI/CD", "GitHub",
                    "Active", "Production", "Healthy",
                    DateTimeOffset.UtcNow.AddMinutes(-12), null,
                    "12m", 48_230),
                new(Guid.Parse("a1b2c3d4-0002-4000-8000-000000000002"),
                    "gitlab-cicd", "GitLab CI/CD", "CI/CD", "GitLab",
                    "Active", "Production", "Healthy",
                    DateTimeOffset.UtcNow.AddMinutes(-8), null,
                    "8m", 31_540),
                new(Guid.Parse("a1b2c3d4-0003-4000-8000-000000000003"),
                    "jira-workitems", "Jira Work Items", "WorkItems", "Atlassian Jira",
                    "Active", "Production", "Healthy",
                    DateTimeOffset.UtcNow.AddMinutes(-5), null,
                    "5m", 124_870),
                new(Guid.Parse("a1b2c3d4-0004-4000-8000-000000000004"),
                    "pagerduty-incidents", "PagerDuty Incidents", "Incidents", "PagerDuty",
                    "Degraded", "Production", "Degraded",
                    DateTimeOffset.UtcNow.AddHours(-2),
                    DateTimeOffset.UtcNow.AddMinutes(-45),
                    "2h 00m", 8_320),
                new(Guid.Parse("a1b2c3d4-0005-4000-8000-000000000005"),
                    "datadog-telemetry", "Datadog Telemetry", "Telemetry", "Datadog",
                    "Active", "Production", "Healthy",
                    DateTimeOffset.UtcNow.AddMinutes(-3), null,
                    "3m", 1_450_000),
                new(Guid.Parse("a1b2c3d4-0006-4000-8000-000000000006"),
                    "kong-gateway", "Kong Gateway", "Gateway", "Kong Inc.",
                    "Active", "Production", "Healthy",
                    DateTimeOffset.UtcNow.AddMinutes(-15), null,
                    "15m", 67_890),
                new(Guid.Parse("a1b2c3d4-0007-4000-8000-000000000007"),
                    "backstage-catalog", "Backstage Catalog", "Catalog", "Spotify Backstage",
                    "Active", "Staging", "Healthy",
                    DateTimeOffset.UtcNow.AddMinutes(-30), null,
                    "30m", 2_450),
                new(Guid.Parse("a1b2c3d4-0008-4000-8000-000000000008"),
                    "kafka-events", "Kafka Events", "EventStream", "Apache Kafka",
                    "Failed", "Production", "Failed",
                    DateTimeOffset.UtcNow.AddHours(-6),
                    DateTimeOffset.UtcNow.AddMinutes(-20),
                    "6h 00m", 890_200),
                new(Guid.Parse("a1b2c3d4-0009-4000-8000-000000000009"),
                    "azuredevops-deployments", "Azure DevOps Deployments", "CI/CD", "Microsoft",
                    "Active", "Production", "Healthy",
                    DateTimeOffset.UtcNow.AddMinutes(-10), null,
                    "10m", 19_750),
                new(Guid.Parse("a1b2c3d4-000a-4000-8000-000000000010"),
                    "swagger-openapi-import", "Swagger/OpenAPI Import", "ContractImport", "OpenAPI Initiative",
                    "Disabled", "Development", "Stale",
                    DateTimeOffset.UtcNow.AddDays(-14), null,
                    "14d", 3_200)
            };

            IEnumerable<ConnectorListItem> filtered = connectors;

            if (!string.IsNullOrEmpty(request.ConnectorType))
                filtered = filtered.Where(c =>
                    c.ConnectorType.Equals(request.ConnectorType, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(request.Status))
                filtered = filtered.Where(c =>
                    c.Status.Equals(request.Status, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(request.Environment))
                filtered = filtered.Where(c =>
                    c.Environment.Equals(request.Environment, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(request.Search))
                filtered = filtered.Where(c =>
                    c.Name.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                    c.DisplayName.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                    c.Provider.Contains(request.Search, StringComparison.OrdinalIgnoreCase));

            var list = filtered.ToList();
            var total = list.Count;
            var paged = list
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var response = new Response(
                TotalCount: total,
                Page: request.Page,
                PageSize: request.PageSize,
                Items: paged);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta paginada com lista de conectores de integração.</summary>
    public sealed record Response(
        int TotalCount,
        int Page,
        int PageSize,
        IReadOnlyList<ConnectorListItem> Items);

    /// <summary>DTO resumido de um conector de integração.</summary>
    public sealed record ConnectorListItem(
        Guid ConnectorId,
        string Name,
        string DisplayName,
        string ConnectorType,
        string Provider,
        string Status,
        string Environment,
        string HealthStatus,
        DateTimeOffset? LastSuccessAt,
        DateTimeOffset? LastFailureAt,
        string? FreshnessLag,
        long ItemsSyncedTotal);
}
