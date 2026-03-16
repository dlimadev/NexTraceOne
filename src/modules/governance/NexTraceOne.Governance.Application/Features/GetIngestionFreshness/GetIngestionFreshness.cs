using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetIngestionFreshness;

/// <summary>
/// Feature: GetIngestionFreshness — detalhe de frescura por fonte de ingestão.
/// Permite visualizar o estado de frescura de cada feed por domínio, conector e tipo de fonte.
/// </summary>
public static class GetIngestionFreshness
{
    /// <summary>Query para obter frescura das fontes de ingestão. Filtro opcional por domínio.</summary>
    public sealed record Query(string? DataDomain = null) : IQuery<Response>;

    /// <summary>Handler que retorna o detalhe de frescura por fonte de ingestão.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;

            var items = new List<FreshnessItem>
            {
                new("Changes", "GitHub CI/CD", "Webhook",
                    "Fresh", now.AddMinutes(-12), 12, "Verified", "Active"),
                new("Changes", "Azure DevOps Deployments", "Webhook",
                    "Fresh", now.AddMinutes(-10), 10, "Trusted", "Active"),
                new("Incidents", "PagerDuty Incidents", "Webhook",
                    "Acceptable", now.AddHours(-2), 120, "Verified", "Active"),
                new("Telemetry", "Datadog Telemetry", "Stream",
                    "Fresh", now.AddMinutes(-3), 3, "Verified", "Active"),
                new("Telemetry", "Datadog Telemetry", "API Polling",
                    "Fresh", now.AddMinutes(-4), 4, "Verified", "Active"),
                new("Contracts", "Kong Gateway", "API Polling",
                    "Fresh", now.AddMinutes(-15), 15, "Trusted", "Active"),
                new("Contracts", "Swagger/OpenAPI Import", "FileImport",
                    "Unknown", now.AddDays(-14), 20_160, "Untrusted", "Paused"),
                new("Knowledge", "Jira Work Items", "API Polling",
                    "Fresh", now.AddMinutes(-5), 5, "Trusted", "Active"),
                new("Knowledge", "Backstage Catalog", "API Polling",
                    "Acceptable", now.AddMinutes(-30), 30, "Trusted", "Active"),
                new("Runtime", "Kafka Events", "Stream",
                    "Stale", now.AddHours(-6), 360, "Provisional", "Failed"),
                new("Alerts", "Datadog Telemetry", "API Polling",
                    "Fresh", now.AddMinutes(-4), 4, "Verified", "Active")
            };

            IEnumerable<FreshnessItem> filtered = items;

            if (!string.IsNullOrEmpty(request.DataDomain))
                filtered = filtered.Where(f =>
                    f.Domain.Equals(request.DataDomain, StringComparison.OrdinalIgnoreCase));

            var list = filtered.ToList();

            var response = new Response(Items: list);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com lista de itens de frescura por fonte de ingestão.</summary>
    public sealed record Response(IReadOnlyList<FreshnessItem> Items);

    /// <summary>DTO de frescura de uma fonte de ingestão.</summary>
    public sealed record FreshnessItem(
        string Domain,
        string ConnectorName,
        string SourceType,
        string Freshness,
        DateTimeOffset? LastReceivedAt,
        long LagMinutes,
        string TrustLevel,
        string Status);
}
