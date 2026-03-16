using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using GetScopedContextFeature = NexTraceOne.Governance.Application.Features.GetScopedContext.GetScopedContext;

namespace NexTraceOne.Governance.API.Endpoints;

/// <summary>
/// Endpoint de contexto de governança do utilizador autenticado.
/// Retorna equipas, domínios, scopes de administração e persona para segmentação da experiência.
/// Essencial para a adaptação da UI por persona no NexTraceOne.
/// </summary>
public sealed class ScopedContextEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/me");

        // ── Contexto de governança do utilizador atual ──
        group.MapGet("/context", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetScopedContextFeature.Query();
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:teams:read");
    }
}
