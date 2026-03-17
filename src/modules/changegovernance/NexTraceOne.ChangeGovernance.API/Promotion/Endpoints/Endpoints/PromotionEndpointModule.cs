using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using ApprovePromotionFeature = NexTraceOne.ChangeGovernance.Application.Promotion.Features.ApprovePromotion.ApprovePromotion;
using BlockPromotionFeature = NexTraceOne.ChangeGovernance.Application.Promotion.Features.BlockPromotion.BlockPromotion;
using ConfigureEnvironmentFeature = NexTraceOne.ChangeGovernance.Application.Promotion.Features.ConfigureEnvironment.ConfigureEnvironment;
using CreatePromotionRequestFeature = NexTraceOne.ChangeGovernance.Application.Promotion.Features.CreatePromotionRequest.CreatePromotionRequest;
using EvaluatePromotionGatesFeature = NexTraceOne.ChangeGovernance.Application.Promotion.Features.EvaluatePromotionGates.EvaluatePromotionGates;
using GetGateEvaluationFeature = NexTraceOne.ChangeGovernance.Application.Promotion.Features.GetGateEvaluation.GetGateEvaluation;
using GetPromotionStatusFeature = NexTraceOne.ChangeGovernance.Application.Promotion.Features.GetPromotionStatus.GetPromotionStatus;
using ListPromotionRequestsFeature = NexTraceOne.ChangeGovernance.Application.Promotion.Features.ListPromotionRequests.ListPromotionRequests;
using OverrideGateFeature = NexTraceOne.ChangeGovernance.Application.Promotion.Features.OverrideGateWithJustification.OverrideGateWithJustification;

namespace NexTraceOne.ChangeGovernance.API.Promotion.Endpoints.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo Promotion.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// </summary>
public sealed class PromotionEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/promotion");

        group.MapPost("/environments", async (
            ConfigureEnvironmentFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/v1/promotion/environments/{0}", localizer);
        })
        .RequirePermission("promotion:environments:write");

        group.MapPost("/requests", async (
            CreatePromotionRequestFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/v1/promotion/requests/{0}", localizer);
        })
        .RequirePermission("promotion:requests:write");

        group.MapPost("/requests/{id:guid}/evaluate-gates", async (
            Guid id,
            EvaluatePromotionGatesFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updated = command with { PromotionRequestId = id };
            var result = await sender.Send(updated, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("promotion:requests:write");

        group.MapPost("/requests/{id:guid}/approve", async (
            Guid id,
            ApprovePromotionFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updated = command with { PromotionRequestId = id };
            var result = await sender.Send(updated, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("promotion:requests:write");

        group.MapPost("/requests/{id:guid}/block", async (
            Guid id,
            BlockPromotionFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updated = command with { PromotionRequestId = id };
            var result = await sender.Send(updated, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("promotion:requests:write");

        group.MapGet("/requests/{id:guid}/status", async (
            Guid id,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetPromotionStatusFeature.Query(id), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("promotion:requests:read");

        group.MapGet("/requests/{id:guid}/gate-evaluations", async (
            Guid id,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetGateEvaluationFeature.Query(id), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("promotion:requests:read");

        group.MapGet("/requests", async (
            string? statusFilter,
            int page,
            int pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListPromotionRequestsFeature.Query(statusFilter, page, pageSize),
                cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("promotion:requests:read");

        group.MapPost("/gate-evaluations/{id:guid}/override", async (
            Guid id,
            OverrideGateFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updated = command with { GateEvaluationId = id };
            var result = await sender.Send(updated, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("promotion:gates:override");
    }
}
