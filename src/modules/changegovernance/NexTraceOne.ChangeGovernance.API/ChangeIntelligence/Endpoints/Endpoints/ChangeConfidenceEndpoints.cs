using MediatR;
using Microsoft.AspNetCore.Builder;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using NexTraceOne.ChangeIntelligence.Domain.Entities;
using NexTraceOne.ChangeIntelligence.Domain.Enums;
using ListChangesFeature = NexTraceOne.ChangeIntelligence.Application.Features.ListChanges.ListChanges;
using GetChangesSummaryFeature = NexTraceOne.ChangeIntelligence.Application.Features.GetChangesSummary.GetChangesSummary;
using ListChangesByServiceFeature = NexTraceOne.ChangeIntelligence.Application.Features.ListChangesByService.ListChangesByService;
using GetReleaseFeature = NexTraceOne.ChangeIntelligence.Application.Features.GetRelease.GetRelease;
using GetBlastRadiusFeature = NexTraceOne.ChangeIntelligence.Application.Features.GetBlastRadiusReport.GetBlastRadiusReport;
using GetIntelligenceSummaryFeature = NexTraceOne.ChangeIntelligence.Application.Features.GetChangeIntelligenceSummary.GetChangeIntelligenceSummary;
using GetChangeAdvisoryFeature = NexTraceOne.ChangeIntelligence.Application.Features.GetChangeAdvisory.GetChangeAdvisory;
using RecordChangeDecisionFeature = NexTraceOne.ChangeIntelligence.Application.Features.RecordChangeDecision.RecordChangeDecision;
using GetChangeDecisionHistoryFeature = NexTraceOne.ChangeIntelligence.Application.Features.GetChangeDecisionHistory.GetChangeDecisionHistory;

namespace NexTraceOne.ChangeIntelligence.API.Endpoints;

/// <summary>
/// Endpoints de Change Confidence do NexTraceOne.
/// Fornecem a experiência de catálogo de mudanças com filtros avançados,
/// resumo agregado e visualização por serviço.
///
/// Disponíveis sob /api/v1/changes para separação clara do conceito de Release.
///
/// Política de autorização:
/// - Leitura: change-intelligence:read.
/// </summary>
internal static class ChangeConfidenceEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de Change Confidence num grupo dedicado.
    /// </summary>
    internal static void Map(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/changes");

        // ── Catálogo de mudanças ────────────────────────────────────

        group.MapGet("/", async (
            string? serviceName,
            string? teamName,
            string? environment,
            ChangeType? changeType,
            ConfidenceStatus? confidenceStatus,
            DeploymentStatus? deploymentStatus,
            string? searchTerm,
            DateTimeOffset? from,
            DateTimeOffset? to,
            int page,
            int pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListChangesFeature.Query(
                    serviceName, teamName, environment,
                    changeType, confidenceStatus, deploymentStatus,
                    searchTerm, from, to, page, pageSize),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:read");

        // ── Resumo agregado ─────────────────────────────────────────

        group.MapGet("/summary", async (
            string? teamName,
            string? environment,
            DateTimeOffset? from,
            DateTimeOffset? to,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetChangesSummaryFeature.Query(teamName, environment, from, to),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:read");

        // ── Mudanças por serviço ────────────────────────────────────

        group.MapGet("/by-service/{serviceName}", async (
            string serviceName,
            int page,
            int pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListChangesByServiceFeature.Query(serviceName, page, pageSize),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:read");

        // ── Detalhe de mudança ──────────────────────────────────────

        group.MapGet("/{changeId:guid}", async (
            Guid changeId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetReleaseFeature.Query(changeId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:read");

        // ── Blast radius de mudança ─────────────────────────────────

        group.MapGet("/{changeId:guid}/blast-radius", async (
            Guid changeId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetBlastRadiusFeature.Query(changeId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:read");

        // ── Intelligence summary de mudança ─────────────────────────

        group.MapGet("/{changeId:guid}/intelligence", async (
            Guid changeId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetIntelligenceSummaryFeature.Query(changeId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:read");

        // ── Advisory de confiança da mudança ────────────────────────

        group.MapGet("/{changeId:guid}/advisory", async (
            Guid changeId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetChangeAdvisoryFeature.Query(changeId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:read");

        // ── Registar decisão de governança ──────────────────────────

        group.MapPost("/{changeId:guid}/decision", async (
            Guid changeId,
            RecordChangeDecisionFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var effectiveCommand = command with { ReleaseId = changeId };
            var result = await sender.Send(effectiveCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:write");

        // ── Histórico de decisões de governança ─────────────────────

        group.MapGet("/{changeId:guid}/decisions", async (
            Guid changeId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetChangeDecisionHistoryFeature.Query(changeId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:read");
    }
}
