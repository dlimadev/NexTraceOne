using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using GetPostReleaseReviewFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetPostReleaseReview.GetPostReleaseReview;
using RecordObservationMetricsFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RecordObservationMetrics.RecordObservationMetrics;
using RegisterExternalMarkerFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RegisterExternalMarker.RegisterExternalMarker;
using GetChangeIntelligenceSummaryFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeIntelligenceSummary.GetChangeIntelligenceSummary;
using RecordReleaseBaselineFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RecordReleaseBaseline.RecordReleaseBaseline;
using StartPostReleaseReviewFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.StartPostReleaseReview.StartPostReleaseReview;
using ProgressPostReleaseReviewFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ProgressPostReleaseReview.ProgressPostReleaseReview;
using AssessRollbackViabilityFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.AssessRollbackViability.AssessRollbackViability;

namespace NexTraceOne.ChangeGovernance.API.ChangeIntelligence.Endpoints.Endpoints;

/// <summary>
/// Endpoints de inteligência avançada de mudança.
/// Agrupa operações de marcadores externos, sumário de inteligência,
/// baseline de indicadores, review pós-release e avaliação de rollback.
///
/// Estes endpoints suportam o fluxo completo de Change Intelligence Record,
/// desde a captura de eventos de CI/CD até a avaliação progressiva pós-deploy.
/// </summary>
internal static class IntelligenceEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de inteligência avançada no grupo raiz de releases.
    /// </summary>
    internal static void Map(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        group.MapPost("/{releaseId:guid}/markers", async (
            Guid releaseId,
            RegisterExternalMarkerFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { ReleaseId = releaseId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:write");

        group.MapGet("/{releaseId:guid}/intelligence", async (
            Guid releaseId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetChangeIntelligenceSummaryFeature.Query(releaseId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:read");

        group.MapPost("/{releaseId:guid}/baseline", async (
            Guid releaseId,
            RecordReleaseBaselineFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { ReleaseId = releaseId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:write");

        group.MapPost("/{releaseId:guid}/review/start", async (
            Guid releaseId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new StartPostReleaseReviewFeature.Command(releaseId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:write");

        group.MapPost("/{releaseId:guid}/review/progress", async (
            Guid releaseId,
            ProgressPostReleaseReviewFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { ReleaseId = releaseId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:write");

        group.MapPost("/{releaseId:guid}/rollback-assessment", async (
            Guid releaseId,
            AssessRollbackViabilityFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { ReleaseId = releaseId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:write");

        // ── Post-change verification endpoints (P5.5) ─────────────────────────

        group.MapPost("/{releaseId:guid}/observation-window", async (
            Guid releaseId,
            RecordObservationMetricsFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { ReleaseId = releaseId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:write");

        group.MapGet("/{releaseId:guid}/review", async (
            Guid releaseId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetPostReleaseReviewFeature.Query(releaseId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:read");
    }
}
