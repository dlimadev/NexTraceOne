using MediatR;
using Microsoft.AspNetCore.Builder;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using GetDeveloperNpsSummaryFeature = NexTraceOne.Catalog.Application.DeveloperExperience.Features.GetDeveloperNpsSummary.GetDeveloperNpsSummary;
using SubmitDeveloperSurveyFeature = NexTraceOne.Catalog.Application.DeveloperExperience.Features.SubmitDeveloperSurvey.SubmitDeveloperSurvey;

namespace NexTraceOne.Catalog.API.DeveloperExperience.Endpoints;

/// <summary>
/// Registra endpoints do subdomínio Developer Survey e NPS (Phase 5.2B).
/// POST /api/v1/developer-experience/surveys — submete survey de NPS.
/// GET  /api/v1/developer-experience/surveys/nps-summary — obtém resumo NPS agregado por equipa.
/// </summary>
public sealed class DeveloperSurveyEndpointModule
{
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/developer-experience").RequireRateLimiting("catalog");

        group.MapPost("/surveys", async (
            SubmitDeveloperSurveyFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:write");

        group.MapGet("/surveys/nps-summary", async (
            string teamId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct,
            string? period = null,
            int page = 1,
            int pageSize = 20) =>
        {
            var query = new GetDeveloperNpsSummaryFeature.Query(teamId, period, page, pageSize);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:read");
    }
}
