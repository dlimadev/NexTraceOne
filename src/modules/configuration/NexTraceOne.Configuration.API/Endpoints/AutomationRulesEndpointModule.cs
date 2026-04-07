using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;

using ListFeature = NexTraceOne.Configuration.Application.Features.ListAutomationRules.ListAutomationRules;
using CreateFeature = NexTraceOne.Configuration.Application.Features.CreateAutomationRule.CreateAutomationRule;
using ToggleFeature = NexTraceOne.Configuration.Application.Features.ToggleAutomationRule.ToggleAutomationRule;
using DeleteFeature = NexTraceOne.Configuration.Application.Features.DeleteAutomationRule.DeleteAutomationRule;

namespace NexTraceOne.Configuration.API.Endpoints;

/// <summary>Endpoints de regras de automação If-Then por tenant.</summary>
public sealed class AutomationRulesEndpointModule
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/automation-rules").RequireAuthorization();

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

        group.MapPatch("/{id:guid}/toggle", async (
            Guid id,
            ToggleFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var cmd = command with { RuleId = id };
            var result = await sender.Send(cmd, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeleteFeature.Command(id), cancellationToken);
            return result.ToHttpResult(localizer);
        });
    }
}
