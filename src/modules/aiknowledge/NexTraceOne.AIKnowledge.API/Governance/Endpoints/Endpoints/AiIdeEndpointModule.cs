using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using GetIdeCapabilitiesFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.GetIdeCapabilities.GetIdeCapabilities;
using ListIdeClientsFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ListIdeClients.ListIdeClients;
using RegisterIdeClientFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.RegisterIdeClient.RegisterIdeClient;
using ListIdeCapabilityPoliciesFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.ListIdeCapabilityPolicies.ListIdeCapabilityPolicies;
using GetIdeSummaryFeature = NexTraceOne.AIKnowledge.Application.Governance.Features.GetIdeSummary.GetIdeSummary;

namespace NexTraceOne.AIKnowledge.API.Governance.Endpoints.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo IDE Integrations.
/// Fornece acesso a gestão de clientes IDE, capacidades, políticas e resumo administrativo.
///
/// Política de autorização:
/// - Leitura IDE: "ai:ide:read" para consulta de capacidades, clientes e políticas.
/// - Escrita IDE: "ai:ide:write" para registo de clientes.
/// - Assistente escrita: "ai:assistant:write" para envio de mensagens via IDE (reutiliza core).
/// </summary>
public sealed class AiIdeEndpointModule
{
    /// <summary>Registra endpoints IDE no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        MapIdeCapabilitiesEndpoints(app);
        MapIdeClientEndpoints(app);
        MapIdePolicyEndpoints(app);
        MapIdeSummaryEndpoints(app);
    }

    // ── IDE Capabilities ────────────────────────────────────────────────

    private static void MapIdeCapabilitiesEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/ide/capabilities");

        group.MapGet("/", async (
            string clientType,
            string? persona,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetIdeCapabilitiesFeature.Query(clientType, persona),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:ide:read");
    }

    // ── IDE Client Registration ─────────────────────────────────────────

    private static void MapIdeClientEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/ide/clients");

        group.MapGet("/", async (
            string? userId,
            string? clientType,
            bool? isActive,
            int? pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListIdeClientsFeature.Query(userId, clientType, isActive, pageSize ?? 50),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:ide:read");

        group.MapPost("/register", async (
            RegisterIdeClientFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:ide:write");
    }

    // ── IDE Capability Policies ─────────────────────────────────────────

    private static void MapIdePolicyEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/ide/policies");

        group.MapGet("/", async (
            string? clientType,
            bool? isActive,
            int? pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListIdeCapabilityPoliciesFeature.Query(clientType, isActive, pageSize ?? 50),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:ide:read");
    }

    // ── IDE Summary (Admin) ─────────────────────────────────────────────

    private static void MapIdeSummaryEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/ide");

        group.MapGet("/summary", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetIdeSummaryFeature.Query(),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:ide:read");
    }
}
