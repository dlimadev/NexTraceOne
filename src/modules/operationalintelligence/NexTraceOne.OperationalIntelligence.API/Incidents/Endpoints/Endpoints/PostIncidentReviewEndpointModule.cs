using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetPostIncidentReview;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ProgressPostIncidentReview;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.StartPostIncidentReview;

namespace NexTraceOne.OperationalIntelligence.API.Incidents.Endpoints.Endpoints;

/// <summary>
/// Endpoints de Post-Incident Review (PIR).
/// Fornece acesso à criação, progressão e consulta de revisões pós-incidente formais.
/// Integra-se com o subdomínio de Incidents e Change Intelligence para análise de causa raiz.
/// </summary>
public sealed class PostIncidentReviewEndpointModule
{
    /// <summary>Mapeia os endpoints de PIR no pipeline HTTP.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/incidents")
            .WithTags("Post-Incident Reviews")
            .WithDescription("Formal post-incident review (PIR) workflow");

        // ── POST /api/v1/incidents/{incidentId}/pir — Iniciar PIR ──
        group.MapPost("/{incidentId:guid}/pir", async (
            Guid incidentId,
            StartPostIncidentReview.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var effectiveCommand = command with { IncidentId = incidentId };
            var result = await sender.Send(effectiveCommand, cancellationToken);
            return result.ToCreatedResult(
                r => $"/api/v1/incidents/{r.IncidentId}/pir",
                localizer);
        })
        .RequirePermission("operations:incidents:write")
        .WithName("StartPostIncidentReview")
        .WithSummary("Start a formal Post-Incident Review (PIR) for a resolved incident");

        // ── GET /api/v1/incidents/{incidentId}/pir — Consultar PIR ──
        group.MapGet("/{incidentId:guid}/pir", async (
            Guid incidentId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPostIncidentReview.Query(incidentId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:incidents:read")
        .WithName("GetPostIncidentReview")
        .WithSummary("Get the Post-Incident Review (PIR) for a specific incident");

        // ── PUT /api/v1/incidents/{incidentId}/pir/progress — Avançar PIR ──
        group.MapPut("/{incidentId:guid}/pir/progress", async (
            Guid incidentId,
            ProgressPostIncidentReview.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:incidents:write")
        .WithName("ProgressPostIncidentReview")
        .WithSummary("Progress a PIR to the next phase with analysis data");
    }
}
