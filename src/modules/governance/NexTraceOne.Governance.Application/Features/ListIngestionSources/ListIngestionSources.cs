using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.ListIngestionSources;

/// <summary>
/// Feature: ListIngestionSources — lista fontes de ingestão registadas.
/// Cada fonte está associada a um conector e tem domínio de dados, nível de confiança e estado de frescura.
/// </summary>
public static class ListIngestionSources
{
    /// <summary>Query para listar fontes de ingestão com filtros e paginação.</summary>
    public sealed record Query(
        Guid? ConnectorId = null,
        string? DataDomain = null,
        string? TrustLevel = null,
        string? Status = null,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Handler que retorna a lista paginada de fontes de ingestão.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var sources = new List<IngestionSourceItem>
            {
                new(Guid.Parse("b1b2b3b4-0001-4000-8000-000000000001"),
                    Guid.Parse("a1b2c3d4-0001-4000-8000-000000000001"),
                    "GitHub CI/CD", "Webhook", "Changes",
                    "Verified", "Fresh",
                    DateTimeOffset.UtcNow.AddMinutes(-12),
                    DateTimeOffset.UtcNow.AddMinutes(-11),
                    "Active",
                    "GitHub Actions workflow runs via webhook and polling fallback"),
                new(Guid.Parse("b1b2b3b4-0002-4000-8000-000000000002"),
                    Guid.Parse("a1b2c3d4-0003-4000-8000-000000000003"),
                    "Jira Work Items", "API Polling", "Knowledge",
                    "Trusted", "Fresh",
                    DateTimeOffset.UtcNow.AddMinutes(-5),
                    DateTimeOffset.UtcNow.AddMinutes(-4),
                    "Active",
                    "Jira REST API polling every 5 minutes for project boards"),
                new(Guid.Parse("b1b2b3b4-0003-4000-8000-000000000003"),
                    Guid.Parse("a1b2c3d4-0004-4000-8000-000000000004"),
                    "PagerDuty Incidents", "Webhook", "Incidents",
                    "Verified", "Acceptable",
                    DateTimeOffset.UtcNow.AddHours(-2),
                    DateTimeOffset.UtcNow.AddHours(-1).AddMinutes(-55),
                    "Active",
                    "PagerDuty webhook events for incident lifecycle"),
                new(Guid.Parse("b1b2b3b4-0004-4000-8000-000000000004"),
                    Guid.Parse("a1b2c3d4-0005-4000-8000-000000000005"),
                    "Datadog Telemetry", "Stream", "Telemetry",
                    "Verified", "Fresh",
                    DateTimeOffset.UtcNow.AddMinutes(-3),
                    DateTimeOffset.UtcNow.AddMinutes(-2),
                    "Active",
                    "Datadog metrics and traces streamed via Datadog Forwarder"),
                new(Guid.Parse("b1b2b3b4-0005-4000-8000-000000000005"),
                    Guid.Parse("a1b2c3d4-0006-4000-8000-000000000006"),
                    "Kong Gateway", "API Polling", "Contracts",
                    "Trusted", "Fresh",
                    DateTimeOffset.UtcNow.AddMinutes(-15),
                    DateTimeOffset.UtcNow.AddMinutes(-14),
                    "Active",
                    "Kong Admin API polling for service and route definitions"),
                new(Guid.Parse("b1b2b3b4-0006-4000-8000-000000000006"),
                    Guid.Parse("a1b2c3d4-0008-4000-8000-000000000008"),
                    "Kafka Events", "Stream", "Runtime",
                    "Provisional", "Stale",
                    DateTimeOffset.UtcNow.AddHours(-6),
                    DateTimeOffset.UtcNow.AddHours(-5).AddMinutes(-50),
                    "Failed",
                    "Kafka consumer group for domain event ingestion — broker unreachable"),
                new(Guid.Parse("b1b2b3b4-0007-4000-8000-000000000007"),
                    Guid.Parse("a1b2c3d4-0009-4000-8000-000000000009"),
                    "Azure DevOps Deployments", "Webhook", "Changes",
                    "Trusted", "Fresh",
                    DateTimeOffset.UtcNow.AddMinutes(-10),
                    DateTimeOffset.UtcNow.AddMinutes(-9),
                    "Active",
                    "Azure DevOps service hooks for release and deployment events"),
                new(Guid.Parse("b1b2b3b4-0008-4000-8000-000000000008"),
                    Guid.Parse("a1b2c3d4-0007-4000-8000-000000000007"),
                    "Backstage Catalog", "API Polling", "Knowledge",
                    "Trusted", "Acceptable",
                    DateTimeOffset.UtcNow.AddMinutes(-30),
                    DateTimeOffset.UtcNow.AddMinutes(-28),
                    "Active",
                    "Backstage Catalog API polling for entity sync"),
                new(Guid.Parse("b1b2b3b4-0009-4000-8000-000000000009"),
                    Guid.Parse("a1b2c3d4-0005-4000-8000-000000000005"),
                    "Datadog Telemetry", "API Polling", "Alerts",
                    "Verified", "Fresh",
                    DateTimeOffset.UtcNow.AddMinutes(-4),
                    DateTimeOffset.UtcNow.AddMinutes(-3),
                    "Active",
                    "Datadog monitors and alert events via API polling"),
                new(Guid.Parse("b1b2b3b4-000a-4000-8000-000000000010"),
                    Guid.Parse("a1b2c3d4-000a-4000-8000-000000000010"),
                    "Swagger/OpenAPI Import", "FileImport", "Contracts",
                    "Untrusted", "Unknown",
                    DateTimeOffset.UtcNow.AddDays(-14),
                    DateTimeOffset.UtcNow.AddDays(-14),
                    "Paused",
                    "Manual OpenAPI spec file import — connector disabled")
            };

            IEnumerable<IngestionSourceItem> filtered = sources;

            if (request.ConnectorId.HasValue)
                filtered = filtered.Where(s => s.ConnectorId == request.ConnectorId.Value);

            if (!string.IsNullOrEmpty(request.DataDomain))
                filtered = filtered.Where(s =>
                    s.DataDomain.Equals(request.DataDomain, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(request.TrustLevel))
                filtered = filtered.Where(s =>
                    s.TrustLevel.Equals(request.TrustLevel, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(request.Status))
                filtered = filtered.Where(s =>
                    s.Status.Equals(request.Status, StringComparison.OrdinalIgnoreCase));

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

    /// <summary>Resposta paginada com lista de fontes de ingestão.</summary>
    public sealed record Response(
        int TotalCount,
        int Page,
        int PageSize,
        IReadOnlyList<IngestionSourceItem> Items);

    /// <summary>DTO de uma fonte de ingestão com dados de proveniência e confiança.</summary>
    public sealed record IngestionSourceItem(
        Guid SourceId,
        Guid ConnectorId,
        string ConnectorName,
        string SourceType,
        string DataDomain,
        string TrustLevel,
        string Freshness,
        DateTimeOffset? LastReceivedAt,
        DateTimeOffset? LastProcessedAt,
        string Status,
        string ProvenanceSummary);
}
