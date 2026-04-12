using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;

using ListFeature = NexTraceOne.Configuration.Application.Features.ListAlertRules.ListAlertRules;
using CreateFeature = NexTraceOne.Configuration.Application.Features.CreateAlertRule.CreateAlertRule;
using ToggleFeature = NexTraceOne.Configuration.Application.Features.ToggleAlertRule.ToggleAlertRule;
using DeleteFeature = NexTraceOne.Configuration.Application.Features.DeleteAlertRule.DeleteAlertRule;

namespace NexTraceOne.Configuration.API.Endpoints;

/// <summary>Endpoints de regras de alerta personalizadas por utilizador.</summary>
public sealed class AlertRulesEndpointModule
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/alert-rules").RequireAuthorization();

        group.MapGet("/", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapPost("/", async (
            CreateFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapPatch("/{ruleId:guid}/toggle", async (
            Guid ruleId,
            ToggleFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var cmd = command with { RuleId = ruleId };
            var result = await sender.Send(cmd, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapDelete("/{ruleId:guid}", async (
            Guid ruleId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeleteFeature.Command(ruleId), cancellationToken);
            return result.ToHttpResult(localizer);
        });
    }
}
