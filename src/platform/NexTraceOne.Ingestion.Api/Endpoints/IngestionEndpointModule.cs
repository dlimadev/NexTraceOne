using NexTraceOne.Ingestion.Api.Security;

namespace NexTraceOne.Ingestion.Api.Endpoints;

/// <summary>
/// Orquestrador de endpoints Minimal API da Ingestion API.
/// Registra todos os grupos de rotas e delega cada domínio funcional
/// para a sua classe de endpoints dedicada.
///
/// Domínios cobertos:
/// - <see cref="DeploymentEventEndpoints"/> — POST /api/v1/deployments/events
/// - <see cref="PromotionEventEndpoints"/>  — POST /api/v1/promotions/events
/// - <see cref="RuntimeSignalEndpoints"/>   — POST /api/v1/runtime/signals
/// - <see cref="ConsumerSyncEndpoints"/>    — POST /api/v1/consumers/sync
/// - <see cref="ContractSyncEndpoints"/>    — POST /api/v1/contracts/sync
/// </summary>
internal static class IngestionEndpointModule
{
    /// <summary>
    /// Ponto de entrada do module — cria os grupos de rotas e delega o registo
    /// de cada endpoint para a classe responsável.
    /// </summary>
    internal static void MapEndpoints(IEndpointRouteBuilder app)
    {
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
    }
}
