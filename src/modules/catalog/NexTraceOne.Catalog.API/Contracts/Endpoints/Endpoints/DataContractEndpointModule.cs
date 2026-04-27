using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using AnalyzeDataContractSchemaFeature = NexTraceOne.Catalog.Application.Contracts.Features.AnalyzeDataContractSchema.AnalyzeDataContractSchema;
using GetDataContractSchemaHistoryFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetDataContractSchemaHistory.GetDataContractSchemaHistory;
using GetContractConsumerInventoryFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetContractConsumerInventory.GetContractConsumerInventory;
using ProposeBreakingChangeFeature = NexTraceOne.Catalog.Application.Contracts.Features.ProposeBreakingChange.ProposeBreakingChange;

namespace NexTraceOne.Catalog.API.Contracts.Endpoints.Endpoints;

/// <summary>
/// Endpoints para Data Contract Schema (CC-03), Consumer Inventory (CC-04)
/// e Breaking Change Proposals (CC-06).
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// </summary>
public sealed class DataContractEndpointModule
{
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        // ── CC-03: Data Contract Schema Analysis ─────────────────────────
        var schemaGroup = app.MapGroup("/api/v1/contracts/data-schemas");

        schemaGroup.MapPost("/", async (
            AnalyzeDataContractSchemaRequest req,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new AnalyzeDataContractSchemaFeature.Command(
                    req.TenantId,
                    req.ApiAssetId,
                    req.Owner,
                    req.SlaFreshnessHours,
                    req.SchemaJson,
                    req.SourceSystem),
                ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("contracts:write")
        .WithTags("Data Contract Schema CC-03")
        .WithSummary("Analyze and register a data contract schema snapshot");

        schemaGroup.MapGet("/{apiAssetId:guid}/history", async (
            Guid apiAssetId,
            string tenantId,
            int? limit,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GetDataContractSchemaHistoryFeature.Query(tenantId, apiAssetId, limit ?? 20),
                ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("contracts:read")
        .WithTags("Data Contract Schema CC-03")
        .WithSummary("Get schema version history for a data contract");

        // ── CC-04: Contract Consumer Inventory ───────────────────────────
        var consumerGroup = app.MapGroup("/api/v1/contracts/consumer-inventory");

        consumerGroup.MapGet("/{contractId:guid}", async (
            Guid contractId,
            string tenantId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GetContractConsumerInventoryFeature.Query(tenantId, contractId),
                ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("contracts:read")
        .WithTags("Contract Consumer Inventory CC-04")
        .WithSummary("Get real consumers of a contract derived from OTel data");

        // ── CC-06: Breaking Change Proposals ─────────────────────────────
        var proposalGroup = app.MapGroup("/api/v1/contracts/breaking-change-proposals");

        proposalGroup.MapPost("/", async (
            ProposeBreakingChangeRequest req,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new ProposeBreakingChangeFeature.Command(
                    req.TenantId,
                    req.ContractId,
                    req.ProposedBreakingChangesJson,
                    req.MigrationWindowDays,
                    req.ProposedBy,
                    req.OpenConsultationImmediately),
                ct);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("contracts:write")
        .WithTags("Breaking Change Proposals CC-06")
        .WithSummary("Propose a breaking change for a contract");
    }
}

public sealed record AnalyzeDataContractSchemaRequest(
    string TenantId,
    Guid ApiAssetId,
    string Owner,
    int SlaFreshnessHours,
    string SchemaJson,
    string SourceSystem);

public sealed record ProposeBreakingChangeRequest(
    string TenantId,
    Guid ContractId,
    string ProposedBreakingChangesJson,
    int MigrationWindowDays,
    string ProposedBy,
    bool OpenConsultationImmediately = true);
