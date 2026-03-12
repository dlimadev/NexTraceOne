using MediatR;
using Microsoft.AspNetCore.Builder;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using ApprovePromotionFeature = NexTraceOne.Promotion.Application.Features.ApprovePromotion.ApprovePromotion;
using BlockPromotionFeature = NexTraceOne.Promotion.Application.Features.BlockPromotion.BlockPromotion;
using ConfigureEnvironmentFeature = NexTraceOne.Promotion.Application.Features.ConfigureEnvironment.ConfigureEnvironment;
using CreatePromotionRequestFeature = NexTraceOne.Promotion.Application.Features.CreatePromotionRequest.CreatePromotionRequest;
using EvaluatePromotionGatesFeature = NexTraceOne.Promotion.Application.Features.EvaluatePromotionGates.EvaluatePromotionGates;
using GetGateEvaluationFeature = NexTraceOne.Promotion.Application.Features.GetGateEvaluation.GetGateEvaluation;
using GetPromotionStatusFeature = NexTraceOne.Promotion.Application.Features.GetPromotionStatus.GetPromotionStatus;
using ListPromotionRequestsFeature = NexTraceOne.Promotion.Application.Features.ListPromotionRequests.ListPromotionRequests;
using OverrideGateFeature = NexTraceOne.Promotion.Application.Features.OverrideGateWithJustification.OverrideGateWithJustification;

namespace NexTraceOne.Promotion.API.Endpoints;

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
        });

        group.MapPost("/requests", async (
            CreatePromotionRequestFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/v1/promotion/requests/{0}", localizer);
        });

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
        });

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
        });

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
        });

        group.MapGet("/requests/{id:guid}/status", async (
            Guid id,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetPromotionStatusFeature.Query(id), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapGet("/requests/{id:guid}/gate-evaluations", async (
            Guid id,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetGateEvaluationFeature.Query(id), cancellationToken);
            return result.ToHttpResult(localizer);
        });

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
        });

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
        });
    }
}
