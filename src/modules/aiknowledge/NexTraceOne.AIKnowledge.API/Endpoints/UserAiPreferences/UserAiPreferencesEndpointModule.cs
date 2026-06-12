using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.AIKnowledge.Application.Features.UserAiPreferences.DeleteUserAiPreference;
using NexTraceOne.AIKnowledge.Application.Features.UserAiPreferences.GetUserAiPreferenceByFeature;
using NexTraceOne.AIKnowledge.Application.Features.UserAiPreferences.GetUserAiPreferences;
using NexTraceOne.AIKnowledge.Application.Features.UserAiPreferences.UpsertUserAiPreference;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

namespace NexTraceOne.AIKnowledge.API.Endpoints;

/// <summary>
/// Endpoints para gerenciamento de preferências de IA do usuário.
/// Permite ao usuário escolher: não usar IA, usar IA interna, ou usar produto externo,
/// por funcionalidade ou globalmente.
/// </summary>
public sealed class UserAiPreferencesEndpointModule
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/me/ai-preferences")
            .WithTags("AI - User Preferences")
            .RequireAuthorization();

        // GET /api/v1/me/ai-preferences
        group.MapGet("/", async (
            [FromServices] ISender sender,
            [FromServices] IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetUserAiPreferences.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .WithName("ListUserAiPreferences")
        .WithSummary("Lista preferências de IA do usuário logado")
        .RequirePermission("ai:assistant:read");

        // GET /api/v1/me/ai-preferences/{featureKey}
        group.MapGet("/{featureKey}", async (
            string featureKey,
            [FromServices] ISender sender,
            [FromServices] IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetUserAiPreferenceByFeature.Query(featureKey), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .WithName("GetUserAiPreferenceByFeature")
        .WithSummary("Obtém preferência de IA para uma funcionalidade específica")
        .RequirePermission("ai:assistant:read");

        // PUT /api/v1/me/ai-preferences
        group.MapPut("/", async (
            [FromBody] UpsertUserAiPreference.Command command,
            [FromServices] ISender sender,
            [FromServices] IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .WithName("UpsertUserAiPreference")
        .WithSummary("Cria ou atualiza preferência de IA do usuário")
        .RequirePermission("ai:assistant:write");

        // DELETE /api/v1/me/ai-preferences/{featureKey}
        group.MapDelete("/{featureKey}", async (
            string featureKey,
            [FromServices] ISender sender,
            [FromServices] IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var getResult = await sender.Send(new GetUserAiPreferenceByFeature.Query(featureKey), cancellationToken);
            if (getResult.IsFailure)
                return getResult.ToHttpResult(localizer);

            var deleteResult = await sender.Send(new DeleteUserAiPreference.Command(getResult.Value.Id), cancellationToken);
            return deleteResult.ToHttpResult(localizer);
        })
        .WithName("DeleteUserAiPreference")
        .WithSummary("Remove preferência de IA do usuário (desativa)")
        .RequirePermission("ai:assistant:write");

        // GET /api/v1/me/ai-preferences/availability/{featureKey}
        group.MapGet("/availability/{featureKey}", async (
            string featureKey,
            [FromServices] IAiExecutionGateway gateway,
            CancellationToken cancellationToken) =>
        {
            var status = await gateway.CheckAvailabilityAsync(featureKey, cancellationToken);
            return Results.Ok(new { FeatureKey = featureKey, Status = status.ToString(), StatusCode = (int)status });
        })
        .WithName("CheckAiAvailability")
        .WithSummary("Verifica se IA está disponível para uma funcionalidade")
        .RequirePermission("ai:assistant:read");

        // POST /api/v1/me/ai-preferences/execution-preview
        group.MapPost("/execution-preview", async (
            [FromBody] ExecutionPreviewRequest request,
            [FromServices] IAiExecutionGateway gateway,
            CancellationToken cancellationToken) =>
        {
            var plan = await gateway.PreviewExecutionAsync(
                new AiExecutionRequest(
                    FeatureKey: request.FeatureKey,
                    RequestType: request.RequestType ?? "chat"),
                cancellationToken);
            return Results.Ok(plan);
        })
        .WithName("PreviewAiExecution")
        .WithSummary("Preview de qual provider/modelo seria usado para uma funcionalidade")
        .RequirePermission("ai:assistant:read");
    }
}

internal sealed record ExecutionPreviewRequest(string FeatureKey, string? RequestType);
