using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using NexTraceOne.Governance.Domain.Enums;
using GetFinOpsSummaryFeature = NexTraceOne.Governance.Application.Features.GetFinOpsSummary.GetFinOpsSummary;
using GetServiceFinOpsFeature = NexTraceOne.Governance.Application.Features.GetServiceFinOps.GetServiceFinOps;
using GetTeamFinOpsFeature = NexTraceOne.Governance.Application.Features.GetTeamFinOps.GetTeamFinOps;
using GetDomainFinOpsFeature = NexTraceOne.Governance.Application.Features.GetDomainFinOps.GetDomainFinOps;
using GetWasteSignalsFeature = NexTraceOne.Governance.Application.Features.GetWasteSignals.GetWasteSignals;
using GetEfficiencyIndicatorsFeature = NexTraceOne.Governance.Application.Features.GetEfficiencyIndicators.GetEfficiencyIndicators;
using GetFinOpsTrendsFeature = NexTraceOne.Governance.Application.Features.GetFinOpsTrends.GetFinOpsTrends;

namespace NexTraceOne.Governance.API.Endpoints;

/// <summary>
/// Endpoints de FinOps do módulo Governance.
/// Disponibiliza resumo contextual de custo, desperdício, eficiência e tendências.
/// FinOps no NexTraceOne é ligado a serviço, equipa, domínio e comportamento operacional.
/// </summary>
public sealed class GovernanceFinOpsEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/finops");

        // ── Resumo contextual de FinOps ──
        group.MapGet("/summary", async (
            string? teamId,
            string? domainId,
            string? serviceId,
            string? range,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetFinOpsSummaryFeature.Query(teamId, domainId, serviceId, range);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:finops:read");

        // ── Perfil de FinOps por serviço ──
        group.MapGet("/services/{serviceId}", async (
            string serviceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetServiceFinOpsFeature.Query(serviceId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:finops:read");

        // ── Perfil de FinOps por equipa ──
        group.MapGet("/teams/{teamId}", async (
            string teamId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetTeamFinOpsFeature.Query(teamId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:finops:read");

        // ── Perfil de FinOps por domínio ──
        group.MapGet("/domains/{domainId}", async (
            string domainId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetDomainFinOpsFeature.Query(domainId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:finops:read");

        // ── Sinais de desperdício ──
        group.MapGet("/waste", async (
            string? serviceId,
            string? teamId,
            string? domainId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetWasteSignalsFeature.Query(serviceId, teamId, domainId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:finops:read");

        // ── Indicadores de eficiência ──
        group.MapGet("/efficiency", async (
            string? serviceId,
            string? teamId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetEfficiencyIndicatorsFeature.Query(serviceId, teamId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:finops:read");

        // ── Tendências de custo ──
        group.MapGet("/trends", async (
            CostDimension? dimension,
            string? filterId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetFinOpsTrendsFeature.Query(dimension ?? CostDimension.Service, filterId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:finops:read");
    }
}
