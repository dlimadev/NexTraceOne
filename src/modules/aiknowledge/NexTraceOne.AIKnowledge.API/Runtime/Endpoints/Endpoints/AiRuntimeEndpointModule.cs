using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using ExecuteAiChatFeature = NexTraceOne.AIKnowledge.Application.Runtime.Features.ExecuteAiChat.ExecuteAiChat;
using ListAiProvidersFeature = NexTraceOne.AIKnowledge.Application.Runtime.Features.ListAiProviders.ListAiProviders;
using CheckAiProvidersHealthFeature = NexTraceOne.AIKnowledge.Application.Runtime.Features.CheckAiProvidersHealth.CheckAiProvidersHealth;

namespace NexTraceOne.AIKnowledge.API.Runtime.Endpoints.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo AI Runtime.
/// Fornece acesso à inferência de chat, listagem de providers e verificação de saúde
/// com governança integrada.
///
/// Política de autorização:
/// - Leitura: "ai:runtime:read" para endpoints de consulta de providers e saúde.
/// - Escrita: "ai:runtime:write" para endpoints de execução de inferência.
/// </summary>
public sealed class AiRuntimeEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        MapChatEndpoints(app);
        MapProviderEndpoints(app);
    }

    // ── Chat ─────────────────────────────────────────────────────────────

    private static void MapChatEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai");

        group.MapPost("/chat", async (
            ExecuteAiChatRequest body,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new ExecuteAiChatFeature.Command(
                body.ConversationId,
                body.Message,
                body.PreferredModelId,
                body.SystemPrompt,
                body.Temperature,
                body.MaxTokens);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:write");
    }

    // ── Providers ────────────────────────────────────────────────────────

    private static void MapProviderEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/providers");

        group.MapGet("/", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListAiProvidersFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:read");

        group.MapGet("/health", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new CheckAiProvidersHealthFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("ai:runtime:read");
    }

    // ── Request DTOs ─────────────────────────────────────────────────────

    internal sealed record ExecuteAiChatRequest(
        string Message,
        Guid? ConversationId,
        Guid? PreferredModelId,
        string? SystemPrompt,
        double? Temperature,
        int? MaxTokens);
}
