using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using CreateWorkflowTemplateFeature = NexTraceOne.ChangeGovernance.Application.Workflow.Features.CreateWorkflowTemplate.CreateWorkflowTemplate;
using ListWorkflowTemplatesFeature = NexTraceOne.ChangeGovernance.Application.Workflow.Features.ListWorkflowTemplates.ListWorkflowTemplates;

namespace NexTraceOne.ChangeGovernance.API.Workflow.Endpoints.Endpoints;

/// <summary>
/// Endpoints de gestão de templates de workflow.
/// Templates definem a estrutura (stages, aprovadores, SLAs) que será instanciada
/// quando um workflow é iniciado para uma release ou promoção.
/// </summary>
internal static class TemplateEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de templates no subgrupo <c>/templates</c>.
    /// </summary>
    internal static void Map(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        var templates = group.MapGroup("/templates");

        templates.MapGet("/", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 20) =>
        {
            var result = await sender.Send(new ListWorkflowTemplatesFeature.Query(page, pageSize), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("workflow:templates:read");

        templates.MapPost("/", async (
            CreateWorkflowTemplateFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(r => $"/api/v1/workflow/templates/{r.TemplateId}", localizer);
        }).RequirePermission("workflow:templates:write");
    }
}
