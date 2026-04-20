using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationRecommendationsBySimilarity;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ScoreCorrelationFeatureSet;

namespace NexTraceOne.OperationalIntelligence.API.Incidents.Endpoints.Endpoints;

/// <summary>
/// Endpoints de Correlation Feature Scoring e Mitigation Similarity.
/// Expõe o motor de correlação feature-based e as recomendações de mitigação por similaridade.
/// </summary>
public sealed class CorrelationFeatureEndpointModule
{
    /// <summary>Mapeia os endpoints de correlation features e mitigation similarity no pipeline HTTP.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/incidents")
            .WithTags("Incident Correlation & Mitigation")
            .WithDescription("Correlation feature scoring and similarity-based mitigation recommendations");

        // ── GET /api/v1/incidents/{incidentId}/changes/{changeId}/feature-score ──
        group.MapGet("/{incidentId}/changes/{changeId}/feature-score", async (
            ISender sender,
            IErrorLocalizer localizer,
            string incidentId,
            string changeId,
            CancellationToken cancellationToken = default) =>
        {
            var query = new ScoreCorrelationFeatureSet.Query(Guid.Parse(incidentId), Guid.Parse(changeId));
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:incidents:read")
        .WithName("ScoreCorrelationFeatureSet")
        .WithSummary("Calculate multi-dimensional correlation feature score between an incident and a change");

        // ── GET /api/v1/incidents/{incidentId}/mitigation/similar-recommendations ──
        group.MapGet("/{incidentId}/mitigation/similar-recommendations", async (
            ISender sender,
            IErrorLocalizer localizer,
            string incidentId,
            int maxResults = 5,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetMitigationRecommendationsBySimilarity.Query(incidentId, maxResults);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:incidents:read")
        .WithName("GetMitigationRecommendationsBySimilarity")
        .WithSummary("Get mitigation recommendations based on similarity with previously resolved incidents");
    }
}
