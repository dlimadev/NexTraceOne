using MediatR;
using Microsoft.AspNetCore.Builder;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using PredictServiceFailureFeature = NexTraceOne.OperationalIntelligence.Application.Reliability.Features.PredictServiceFailure.PredictServiceFailure;
using GetCapacityForecastFeature = NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetCapacityForecast.GetCapacityForecast;
using GetSloBurnRateAlertFeature = NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetSloBurnRateAlert.GetSloBurnRateAlert;
using GetChangeRiskPredictionFeature = NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetChangeRiskPrediction.GetChangeRiskPrediction;

namespace NexTraceOne.OperationalIntelligence.API.Predictive.Endpoints.Endpoints;

/// <summary>
/// Registra endpoints de Predictive Intelligence: previsão de falhas, capacidade,
/// burn rate de SLO e risco de mudanças.
/// </summary>
public sealed class PredictiveIntelligenceEndpointModule
{
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/predictions").RequireRateLimiting("operations");

        group.MapPost("/service-failure", async (
            PredictServiceFailureFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("operations:predictions:write");

        group.MapPost("/capacity-forecast", async (
            GetCapacityForecastFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("operations:predictions:write");

        group.MapGet("/slo-burn-rate", async (
            string serviceId,
            string environment,
            decimal currentErrorRatePercent,
            decimal sloTargetPercent,
            string windowHours,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var query = new GetSloBurnRateAlertFeature.Query(
                serviceId, environment, currentErrorRatePercent, sloTargetPercent, windowHours);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("operations:read");

        group.MapGet("/change-risk/{changeId:guid}", async (
            Guid changeId,
            string serviceId,
            string environment,
            int priorIncidentRate,
            decimal blastRadius,
            bool hasTestEvidence,
            bool isBusinessHours,
            string changeType,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var query = new GetChangeRiskPredictionFeature.Query(
                changeId, serviceId, environment, priorIncidentRate,
                blastRadius, hasTestEvidence, isBusinessHours, changeType);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("operations:read");
    }
}
