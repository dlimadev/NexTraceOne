using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using GetOnboardingContextFeature = NexTraceOne.Governance.Application.Features.GetOnboardingContext.GetOnboardingContext;

namespace NexTraceOne.Governance.API.Endpoints;

/// <summary>
/// Endpoints de Onboarding do produto.
/// Fornece contexto de quickstart, acções recomendadas e orientação adaptada por persona.
/// </summary>
public sealed class OnboardingEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/onboarding");

        // ── Contexto de onboarding adaptado à persona ──
        group.MapGet("/context", async (
            string? persona,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetOnboardingContextFeature.Query(persona ?? "Engineer");
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:read");

        // ── Quickstart items por persona ──
        group.MapGet("/quickstart", async (
            string? persona,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetOnboardingContextFeature.Query(persona ?? "Engineer");
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:read");

        // ── Acções recomendadas por contexto ──
        group.MapGet("/recommendations", async (
            string? persona,
            string? scope,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetOnboardingContextFeature.Query(persona ?? "Engineer");
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:reports:read");
    }
}
