using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

using AddDraftExampleFeature = NexTraceOne.Catalog.Application.Contracts.Features.AddDraftExample.AddDraftExample;
using ApproveDraftFeature = NexTraceOne.Catalog.Application.Contracts.Features.ApproveDraft.ApproveDraft;
using CreateDraftFeature = NexTraceOne.Catalog.Application.Contracts.Features.CreateDraft.CreateDraft;
using GenerateDraftFromAiFeature = NexTraceOne.Catalog.Application.Contracts.Features.GenerateDraftFromAi.GenerateDraftFromAi;
using GetDraftFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetDraft.GetDraft;
using ListDraftReviewsFeature = NexTraceOne.Catalog.Application.Contracts.Features.ListDraftReviews.ListDraftReviews;
using ListDraftsFeature = NexTraceOne.Catalog.Application.Contracts.Features.ListDrafts.ListDrafts;
using PublishDraftFeature = NexTraceOne.Catalog.Application.Contracts.Features.PublishDraft.PublishDraft;
using RejectDraftFeature = NexTraceOne.Catalog.Application.Contracts.Features.RejectDraft.RejectDraft;
using SubmitDraftForReviewFeature = NexTraceOne.Catalog.Application.Contracts.Features.SubmitDraftForReview.SubmitDraftForReview;
using UpdateDraftContentFeature = NexTraceOne.Catalog.Application.Contracts.Features.UpdateDraftContent.UpdateDraftContent;
using UpdateDraftMetadataFeature = NexTraceOne.Catalog.Application.Contracts.Features.UpdateDraftMetadata.UpdateDraftMetadata;

namespace NexTraceOne.Catalog.API.Contracts.Endpoints.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do Contract Studio (drafts, revisões, publicação).
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// Inclui CRUD de drafts, fluxo de revisão e aprovação, publicação e geração por IA.
///
/// Política de autorização:
/// - Endpoints de leitura (get, list, reviews) exigem "contracts:read".
/// - Endpoints de escrita (create, update, submit, approve, reject, publish, generate, examples)
///   exigem "contracts:write".
/// </summary>
public sealed class ContractStudioEndpointModule
{
    /// <summary>Registra endpoints do Contract Studio no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/contracts/drafts");

        // ── Drafts CRUD ─────────────────────────────────────────

        group.MapPost(string.Empty, async (
            CreateDraftFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(response => $"/api/v1/contracts/drafts/{response.DraftId}", localizer);
        }).RequirePermission("contracts:write");

        group.MapGet("/{draftId:guid}", async (
            Guid draftId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetDraftFeature.Query(draftId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        group.MapGet(string.Empty, async (
            string? status,
            Guid? serviceId,
            string? author,
            int? page,
            int? pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            DraftStatus? parsedStatus = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<DraftStatus>(status, ignoreCase: true, out var statusValue))
                {
                    return Results.BadRequest(new
                    {
                        code = "Contracts.Draft.InvalidStatus",
                        detail = $"Invalid draft status '{status}'."
                    });
                }

                parsedStatus = statusValue;
            }

            var result = await sender.Send(new ListDraftsFeature.Query(
                parsedStatus, serviceId, author, page ?? 1, pageSize ?? 20), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        group.MapPatch("/{draftId:guid}/content", async (
            Guid draftId,
            UpdateDraftContentFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { DraftId = draftId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:write");

        group.MapPatch("/{draftId:guid}/metadata", async (
            Guid draftId,
            UpdateDraftMetadataFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { DraftId = draftId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:write");

        // ── Review Workflow ─────────────────────────────────────

        group.MapPost("/{draftId:guid}/submit-review", async (
            Guid draftId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new SubmitDraftForReviewFeature.Command(draftId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:write");

        group.MapPost("/{draftId:guid}/approve", async (
            Guid draftId,
            ApproveDraftFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { DraftId = draftId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:write");

        group.MapPost("/{draftId:guid}/reject", async (
            Guid draftId,
            RejectDraftFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { DraftId = draftId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:write");

        group.MapGet("/{draftId:guid}/reviews", async (
            Guid draftId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListDraftReviewsFeature.Query(draftId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        // ── Publication ─────────────────────────────────────────

        group.MapPost("/{draftId:guid}/publish", async (
            Guid draftId,
            PublishDraftFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { DraftId = draftId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:write");

        // ── AI-Assisted Generation ──────────────────────────────

        group.MapPost("/generate", async (
            GenerateDraftFromAiFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(response => $"/api/v1/contracts/drafts/{response.DraftId}", localizer);
        }).RequirePermission("contracts:write");

        // ── Examples ────────────────────────────────────────────

        group.MapPost("/{draftId:guid}/examples", async (
            Guid draftId,
            AddDraftExampleFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { DraftId = draftId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToCreatedResult("/api/v1/contracts/drafts/{0}/examples", localizer);
        }).RequirePermission("contracts:write");
    }
}
