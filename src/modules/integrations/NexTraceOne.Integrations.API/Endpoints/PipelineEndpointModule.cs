using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using NexTraceOne.Integrations.Domain.Enums;

using CreatePipelineRuleFeature = NexTraceOne.Integrations.Application.Features.CreatePipelineRule.CreatePipelineRule;
using ListPipelineRulesFeature = NexTraceOne.Integrations.Application.Features.ListPipelineRules.ListPipelineRules;
using UpdatePipelineRuleFeature = NexTraceOne.Integrations.Application.Features.UpdatePipelineRule.UpdatePipelineRule;
using DeletePipelineRuleFeature = NexTraceOne.Integrations.Application.Features.DeletePipelineRule.DeletePipelineRule;
using CreateStorageBucketFeature = NexTraceOne.Integrations.Application.Features.CreateStorageBucket.CreateStorageBucket;
using ListStorageBucketsFeature = NexTraceOne.Integrations.Application.Features.ListStorageBuckets.ListStorageBuckets;
using CreateLogToMetricRuleFeature = NexTraceOne.Integrations.Application.Features.CreateLogToMetricRule.CreateLogToMetricRule;
using ListLogToMetricRulesFeature = NexTraceOne.Integrations.Application.Features.ListLogToMetricRules.ListLogToMetricRules;

namespace NexTraceOne.Integrations.API.Endpoints;

/// <summary>
/// Endpoints do Pipeline de Ingestão — Pipeline Rules (PIP-03), Storage Buckets (PIP-04),
/// Log-to-Metric Rules (PIP-06).
///
/// Rota base: /api/v1/integrations/pipeline-*
/// Permissões: integrations:read / integrations:write
/// </summary>
public sealed class PipelineEndpointModule
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var pipeline = app.MapGroup("/api/v1/integrations");

        // ── PIP-03: Pipeline Rules ──

        pipeline.MapGet("/pipeline-rules", async (
            string tenantId,
            string? ruleType,
            string? signalType,
            bool? isEnabled,
            int? page,
            int? pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            PipelineRuleType? rt = Enum.TryParse<PipelineRuleType>(ruleType, true, out var rtVal) ? rtVal : null;
            PipelineSignalType? st = Enum.TryParse<PipelineSignalType>(signalType, true, out var stVal) ? stVal : null;

            var query = new ListPipelineRulesFeature.Query(tenantId, rt, st, isEnabled, page ?? 1, pageSize ?? 20);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("integrations:read");

        pipeline.MapPost("/pipeline-rules", async (
            CreatePipelineRuleFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("integrations:write");

        pipeline.MapPut("/pipeline-rules/{id:guid}", async (
            Guid id,
            UpdatePipelineRuleRequestBody body,
            string tenantId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdatePipelineRuleFeature.Command(
                tenantId, id, body.Name, body.RuleType, body.SignalType,
                body.ConditionJson, body.ActionJson, body.Priority, body.IsEnabled, body.Description);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("integrations:write");

        pipeline.MapDelete("/pipeline-rules/{id:guid}", async (
            Guid id,
            string tenantId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new DeletePipelineRuleFeature.Command(tenantId, id);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("integrations:write");

        // ── PIP-04: Storage Buckets ──

        pipeline.MapGet("/storage-buckets", async (
            string tenantId,
            bool? isEnabled,
            int? page,
            int? pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new ListStorageBucketsFeature.Query(tenantId, isEnabled, page ?? 1, pageSize ?? 20);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("integrations:read");

        pipeline.MapPost("/storage-buckets", async (
            CreateStorageBucketFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("integrations:write");

        // ── PIP-06: Log-to-Metric Rules ──

        pipeline.MapGet("/log-metric-rules", async (
            string tenantId,
            bool? isEnabled,
            int? page,
            int? pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new ListLogToMetricRulesFeature.Query(tenantId, isEnabled, page ?? 1, pageSize ?? 20);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("integrations:read");

        pipeline.MapPost("/log-metric-rules", async (
            CreateLogToMetricRuleFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("integrations:write");
    }

    // ── Request body records ──

    private sealed record UpdatePipelineRuleRequestBody(
        string Name,
        PipelineRuleType RuleType,
        PipelineSignalType SignalType,
        string ConditionJson,
        string ActionJson,
        int Priority,
        bool IsEnabled,
        string? Description = null);
}
