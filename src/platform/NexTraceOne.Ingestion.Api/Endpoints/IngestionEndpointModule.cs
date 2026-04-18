using NexTraceOne.Ingestion.Api.Security;

namespace NexTraceOne.Ingestion.Api.Endpoints;

/// <summary>
/// Orquestrador de endpoints Minimal API da Ingestion API.
/// Registra todos os grupos de rotas e delega cada domínio funcional
/// para a sua classe de endpoints dedicada.
///
/// Domínios cobertos — escrita (integrations:write):
/// - <see cref="DeploymentEventEndpoints"/>  — POST /api/v1/deployments/events
/// - <see cref="PromotionEventEndpoints"/>   — POST /api/v1/promotions/events
/// - <see cref="RuntimeSignalEndpoints"/>    — POST /api/v1/runtime/signals
///                                              POST /api/v1/runtime/snapshots
/// - <see cref="ConsumerSyncEndpoints"/>     — POST /api/v1/consumers/sync
/// - <see cref="ContractSyncEndpoints"/>     — POST /api/v1/contracts/sync
/// - <see cref="CommitIngestEndpoints"/>     — POST /api/v1/commits
/// - <see cref="ReleaseIngestEndpoints"/>    — POST /api/v1/releases
///                                              POST /api/v1/releases/feature-flags    (externalReleaseId no body)
///                                              POST /api/v1/releases/canary           (externalReleaseId no body)
///                                              POST /api/v1/releases/observations     (externalReleaseId no body)
///                                              POST /api/v1/releases/rollback         (externalReleaseId no body)
/// - <see cref="CostIngestEndpoints"/>       — POST /api/v1/costs/snapshots
/// - <see cref="IncidentEndpoints"/>         — POST /api/v1/incidents
///
/// Domínios cobertos — leitura (integrations:read):
/// - <see cref="ReleaseQueryEndpoints"/>     — GET  /api/v1/releases
///                                              GET  /api/v1/releases/{id}
///                                              GET  /api/v1/releases/{id}/advisory
///                                              GET  /api/v1/releases/{id}/blast-radius
///                                              GET  /api/v1/releases/{id}/post-release-review
///                                              GET  /api/v1/releases/resolve?externalReleaseId=X&amp;externalSystem=Y
///                                              GET  /api/v1/releases/by-external/{externalSystem}/{externalReleaseId}
///                                              GET  /api/v1/releases/by-external/{externalSystem}/{externalReleaseId}/advisory
///                                              GET  /api/v1/releases/by-external/{externalSystem}/{externalReleaseId}/blast-radius
///                                              GET  /api/v1/releases/by-external/{externalSystem}/{externalReleaseId}/post-release-review
/// - <see cref="ServiceHealthQueryEndpoints"/> — GET /api/v1/services/{serviceName}/health
/// - <see cref="IncidentEndpoints"/>          — GET /api/v1/incidents
///                                              GET /api/v1/incidents/{id}
/// </summary>
internal static class IngestionEndpointModule
{
    /// <summary>
    /// Ponto de entrada do module — cria os grupos de rotas e delega o registo
    /// de cada endpoint para a classe responsável.
    /// </summary>
    internal static void MapEndpoints(IEndpointRouteBuilder app)
    {
        // ── Write groups (integrations:write) ─────────────────────────────────
        var deployments = app.MapGroup("/api/v1/deployments")
            .WithTags("Deployments")
            .RequireAuthorization(IngestionApiSecurity.PolicyName);
        DeploymentEventEndpoints.Map(deployments);

        var promotions = app.MapGroup("/api/v1/promotions")
            .WithTags("Promotions")
            .RequireAuthorization(IngestionApiSecurity.PolicyName);
        PromotionEventEndpoints.Map(promotions);

        var runtime = app.MapGroup("/api/v1/runtime")
            .WithTags("Runtime")
            .RequireAuthorization(IngestionApiSecurity.PolicyName);
        RuntimeSignalEndpoints.Map(runtime);

        var consumers = app.MapGroup("/api/v1/consumers")
            .WithTags("Consumers")
            .RequireAuthorization(IngestionApiSecurity.PolicyName);
        ConsumerSyncEndpoints.Map(consumers);

        var contracts = app.MapGroup("/api/v1/contracts")
            .WithTags("Contracts")
            .RequireAuthorization(IngestionApiSecurity.PolicyName);
        ContractSyncEndpoints.Map(contracts);

        var commits = app.MapGroup("/api/v1/commits")
            .WithTags("Commits")
            .RequireAuthorization(IngestionApiSecurity.PolicyName);
        CommitIngestEndpoints.Map(commits);

        var releasesWrite = app.MapGroup("/api/v1/releases")
            .WithTags("Releases")
            .RequireAuthorization(IngestionApiSecurity.PolicyName);
        ReleaseIngestEndpoints.Map(releasesWrite);

        var costs = app.MapGroup("/api/v1/costs")
            .WithTags("FinOps")
            .RequireAuthorization(IngestionApiSecurity.PolicyName);
        CostIngestEndpoints.Map(costs);

        var incidentsWrite = app.MapGroup("/api/v1/incidents")
            .WithTags("Incidents")
            .RequireAuthorization(IngestionApiSecurity.PolicyName);

        // ── Read groups (integrations:read) ───────────────────────────────────
        var releasesRead = app.MapGroup("/api/v1/releases")
            .WithTags("Releases")
            .RequireAuthorization(IngestionApiSecurity.ReadPolicyName);
        ReleaseQueryEndpoints.Map(releasesRead);

        var services = app.MapGroup("/api/v1/services")
            .WithTags("Services")
            .RequireAuthorization(IngestionApiSecurity.ReadPolicyName);
        ServiceHealthQueryEndpoints.Map(services);

        var incidentsRead = app.MapGroup("/api/v1/incidents")
            .WithTags("Incidents")
            .RequireAuthorization(IngestionApiSecurity.ReadPolicyName);

        // Shared incident endpoints: POST uses write policy, GET uses read policy
        IncidentEndpoints.Map(incidentsWrite, incidentsRead);
    }
}
