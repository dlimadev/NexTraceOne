using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using GetFeatureFlagInventoryReportFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetFeatureFlagInventoryReport.GetFeatureFlagInventoryReport;
using GetServiceFeatureFlagDashboardFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetServiceFeatureFlagDashboard.GetServiceFeatureFlagDashboard;
using ToggleServiceFeatureFlagFeature = NexTraceOne.Catalog.Application.Contracts.Features.ToggleServiceFeatureFlag.ToggleServiceFeatureFlag;
using GetFeatureFlagRiskReportFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetFeatureFlagRiskReport.GetFeatureFlagRiskReport;
using GetSbomCoverageReportFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetSbomCoverageReport.GetSbomCoverageReport;
using IngestFeatureFlagStateFeature = NexTraceOne.Catalog.Application.Contracts.Features.IngestFeatureFlagState.IngestFeatureFlagState;
using IngestSbomRecordFeature = NexTraceOne.Catalog.Application.Contracts.Features.IngestSbomRecord.IngestSbomRecord;
using ScheduleContractDeprecationFeature = NexTraceOne.Catalog.Application.Contracts.Features.ScheduleContractDeprecation.ScheduleContractDeprecation;

namespace NexTraceOne.Catalog.API.Contracts.Endpoints.Endpoints;

/// <summary>
/// Endpoints de operação de contratos: ingestão/cobertura de SBOM,
/// registro de governança de feature flags e agendamento de deprecação.
/// Expõe handlers que existiam na Application sem rota HTTP.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// </summary>
public sealed class ContractOpsEndpointModule
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        // ── SBOM (Software Bill of Materials) ─────────────────────────────
        var sbomGroup = app.MapGroup("/api/v1/contracts/sbom");

        sbomGroup.MapPost("/", async (
            IngestSbomRecordFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("contracts:write")
        .WithTags("SBOM")
        .WithSummary("Ingest an SBOM record for a service version");

        sbomGroup.MapGet("/coverage-report", async (
            string tenantId,
            int? freshDays,
            int? staleDays,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GetSbomCoverageReportFeature.Query(
                    tenantId,
                    freshDays ?? 30,
                    staleDays ?? 90),
                ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("contracts:read")
        .WithTags("SBOM")
        .WithSummary("SBOM coverage report: services with fresh, stale or missing SBOMs");

        // ── Feature Flag Governance Registry ──────────────────────────────
        var flagsGroup = app.MapGroup("/api/v1/contracts/feature-flags");

        // Dashboard agregado consumido pela aba "Feature Flags" do detalhe do serviço.
        // O tenant é resolvido do contexto autenticado (o frontend não envia tenantId).
        flagsGroup.MapGet("/", async (
            ICurrentTenant currentTenant,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GetServiceFeatureFlagDashboardFeature.Query(currentTenant.Id.ToString()), ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("contracts:read")
        .WithTags("Feature Flag Governance")
        .WithSummary("Service feature flag dashboard: flags with current state and global counters");

        // Alterna o estado de uma flag existente (activa/desactiva).
        flagsGroup.MapPatch("/{flagId:guid}", async (
            Guid flagId,
            ToggleServiceFeatureFlagFeature.ToggleBody body,
            ICurrentTenant currentTenant,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new ToggleServiceFeatureFlagFeature.Command(flagId, currentTenant.Id.ToString(), body.Enabled), ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("contracts:write")
        .WithTags("Feature Flag Governance")
        .WithSummary("Toggle a feature flag on/off from the service detail view");

        flagsGroup.MapPost("/", async (
            IngestFeatureFlagStateFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("contracts:write")
        .WithTags("Feature Flag Governance")
        .WithSummary("Ingest the state of a feature flag for governance tracking");

        flagsGroup.MapGet("/inventory-report", async (
            string tenantId,
            int? staleFlagDays,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GetFeatureFlagInventoryReportFeature.Query(
                    tenantId,
                    staleFlagDays ?? 60),
                ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("contracts:read")
        .WithTags("Feature Flag Governance")
        .WithSummary("Feature flag inventory: flags per service, stale and orphaned flags");

        flagsGroup.MapGet("/risk-report", async (
            string tenantId,
            int? staleFlagDays,
            int? prodPresenceDays,
            int? incidentWindowHours,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GetFeatureFlagRiskReportFeature.Query(
                    tenantId,
                    staleFlagDays ?? 60,
                    prodPresenceDays ?? 90,
                    incidentWindowHours ?? 24),
                ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("contracts:read")
        .WithTags("Feature Flag Governance")
        .WithSummary("Feature flag risk report: stale flags, divergence between environments");

        // ── Contract Deprecation Scheduling ───────────────────────────────
        app.MapPost("/api/v1/contracts/{contractId:guid}/schedule-deprecation", async (
            Guid contractId,
            ScheduleContractDeprecationFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var updated = command with { ContractId = contractId };
            var result = await sender.Send(updated, ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("contracts:write")
        .WithTags("Contract Deprecation")
        .WithSummary("Schedule a future deprecation for a contract with sunset date and migration guide");
    }
}
