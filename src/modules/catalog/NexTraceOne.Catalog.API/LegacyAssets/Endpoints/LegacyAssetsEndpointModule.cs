using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using NexTraceOne.Catalog.Domain.LegacyAssets.Enums;
using GetLegacyImpactPropagationFeature = NexTraceOne.Catalog.Application.LegacyAssets.Features.GetLegacyImpactPropagation.GetLegacyImpactPropagation;

namespace NexTraceOne.Catalog.API.LegacyAssets.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do sub-domínio Legacy Assets do módulo Catalog.
/// Descoberto automaticamente pelo ApiHost via assembly scanning (convenção *EndpointModule).
///
/// Política de autorização:
/// - Endpoints de leitura exigem "catalog:legacy-assets:read".
/// - Endpoints de escrita exigem "catalog:legacy-assets:write".
/// </summary>
public sealed class LegacyAssetsEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/catalog/legacy")
            .WithTags("Legacy Assets")
            .RequireRateLimiting("data-intensive");

        // ── Registo de ativos legacy ─────────────────────────────────────
        RegisterMainframeSystemEndpoint.Map(group);
        RegisterCobolProgramEndpoint.Map(group);
        RegisterCopybookEndpoint.Map(group);
        RegisterCicsTransactionEndpoint.Map(group);
        RegisterImsTransactionEndpoint.Map(group);
        RegisterDb2ArtifactEndpoint.Map(group);
        RegisterZosConnectBindingEndpoint.Map(group);

        // ── Consulta de ativos legacy ────────────────────────────────────
        ListLegacyAssetsEndpoint.Map(group);
        GetLegacyAssetDetailEndpoint.Map(group);

        // ── Ingestão bulk de ativos legacy ───────────────────────────────
        SyncLegacyAssetsEndpoint.Map(group);

        // ── Legacy contract governance ───────────────────────────────────
        ImportCopybookLayoutEndpoint.Map(group);
        DiffCopybookVersionsEndpoint.Map(group);

        // ── Análise de impacto no grafo legacy ──────────────────────────
        group.MapGet("/{assetId:guid}/impact", async (
            Guid assetId,
            MainframeAssetType assetType,
            int maxDepth,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetLegacyImpactPropagationFeature.Query(assetId, assetType, maxDepth == 0 ? 3 : maxDepth);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:legacy-assets:read");
    }
}
