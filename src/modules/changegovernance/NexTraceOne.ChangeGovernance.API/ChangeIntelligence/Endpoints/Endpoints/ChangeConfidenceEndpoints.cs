using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

using ListChangesFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListChanges.ListChanges;
using GetChangesSummaryFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangesSummary.GetChangesSummary;
using ListChangesByServiceFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListChangesByService.ListChangesByService;
using GetReleaseFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetRelease.GetRelease;
using GetBlastRadiusFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetBlastRadiusReport.GetBlastRadiusReport;
using GetIntelligenceSummaryFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeIntelligenceSummary.GetChangeIntelligenceSummary;
using GetChangeAdvisoryFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeAdvisory.GetChangeAdvisory;
using RecordChangeDecisionFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RecordChangeDecision.RecordChangeDecision;
using GetChangeDecisionHistoryFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeDecisionHistory.GetChangeDecisionHistory;
using GetDoraMetricsFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetDoraMetrics.GetDoraMetrics;
using NotifyDeploymentFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.NotifyDeployment.NotifyDeployment;

namespace NexTraceOne.ChangeGovernance.API.ChangeIntelligence.Endpoints.Endpoints;

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
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 20) =>
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

        // ── Opções dinâmicas para filtros ─────────────────────────

        group.MapGet("/filter-options", () =>
            Results.Ok(new ChangeFilterOptionsResponse(
                Enum.GetNames<ChangeType>(),
                Enum.GetNames<ConfidenceStatus>(),
                Enum.GetNames<DeploymentStatus>())))
            .RequirePermission("change-intelligence:read");

        // ── Mudanças por serviço ────────────────────────────────────

        group.MapGet("/by-service/{serviceName}", async (
            string serviceName,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 20) =>
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

        // ── DORA Metrics ────────────────────────────────────────────

        group.MapGet("/dora-metrics", async (
            string? serviceName,
            string? teamName,
            string? environment,
            int? days,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetDoraMetricsFeature.Query(
                    serviceName,
                    teamName,
                    environment ?? "Production",
                    days ?? 30),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:read");

        // ── Endpoint de conveniência para ingestão de eventos de deploy via CI/CD ───
        // Alias acessível em /api/v1/changes/deploy-events para facilitar integração
        // com pipelines CI/CD (GitHub Actions, Jenkins, GitLab). O endpoint canónico
        // permanece em POST /api/v1/releases/ (DeploymentEndpoints).
        group.MapPost("/deploy-events", async (
            NotifyDeploymentFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/v1/releases/{0}", localizer);
        }).RequirePermission("change-intelligence:write");
    }

    private sealed record ChangeFilterOptionsResponse(
        string[] ChangeTypes,
        string[] ConfidenceStatuses,
        string[] DeploymentStatuses);
}
