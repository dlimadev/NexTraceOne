using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;

using CreateWorkflowTemplateFeature = NexTraceOne.ChangeGovernance.Application.Workflow.Features.CreateWorkflowTemplate.CreateWorkflowTemplate;

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

        templates.MapPost("/", async (
            CreateWorkflowTemplateFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/v1/workflow/templates/{0}", localizer);
        });
    }
}
