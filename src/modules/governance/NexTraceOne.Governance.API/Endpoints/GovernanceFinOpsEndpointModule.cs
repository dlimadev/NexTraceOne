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
using GetFinOpsConfigurationFeature = NexTraceOne.Governance.Application.Features.GetFinOpsConfiguration.GetFinOpsConfiguration;
using EvaluateReleaseBudgetGateFeature = NexTraceOne.Governance.Application.Features.EvaluateReleaseBudgetGate.EvaluateReleaseBudgetGate;
using CreateFinOpsBudgetApprovalFeature = NexTraceOne.Governance.Application.Features.CreateFinOpsBudgetApproval.CreateFinOpsBudgetApproval;
using ResolveFinOpsBudgetApprovalFeature = NexTraceOne.Governance.Application.Features.ResolveFinOpsBudgetApproval.ResolveFinOpsBudgetApproval;
using ListFinOpsBudgetApprovalsFeature = NexTraceOne.Governance.Application.Features.ListFinOpsBudgetApprovals.ListFinOpsBudgetApprovals;
using GetCostContextPerDayFeature = NexTraceOne.Governance.Application.Features.GetCostContextPerDay.GetCostContextPerDay;

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
        var group = app.MapGroup("/api/v1/finops").RequireRateLimiting("data-intensive");

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

        // ── Configuração FinOps (moeda, gate de orçamento, aprovadores) ──
        group.MapGet("/configuration", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetFinOpsConfigurationFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:finops:read");

        // ── Gate de orçamento por release ──
        group.MapPost("/releases/evaluate-budget-gate", async (
            EvaluateReleaseBudgetGateFeature.Query query,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:finops:read");

        // ── Pedidos de aprovação de orçamento ──
        group.MapGet("/budget-approvals", async (
            string? status,
            string? serviceName,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new ListFinOpsBudgetApprovalsFeature.Query(status, serviceName);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:finops:read");

        group.MapPost("/budget-approvals", async (
            CreateFinOpsBudgetApprovalFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:finops:write");

        group.MapPut("/budget-approvals/{approvalId:guid}/resolve", async (
            Guid approvalId,
            ResolveApprovalRequest body,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new ResolveFinOpsBudgetApprovalFeature.Command(
                approvalId, body.Approved, body.ResolvedBy, body.Comment);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:finops:write");

        // ── Contexto de custo por dia para gate de budget ──
        group.MapGet("/service/{serviceName}/cost-context", async (
            string serviceName,
            string environment,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCostContextPerDayFeature.Query(serviceName, environment);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:finops:read");
    }

    private sealed record ResolveApprovalRequest(bool Approved, string ResolvedBy, string? Comment);
}
