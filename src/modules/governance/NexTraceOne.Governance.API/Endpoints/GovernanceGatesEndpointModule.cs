using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using FourEyesFeature = NexTraceOne.Governance.Application.Features.EvaluateFourEyesPrinciple.EvaluateFourEyesPrinciple;
using CabFeature = NexTraceOne.Governance.Application.Features.EvaluateChangeAdvisoryBoard.EvaluateChangeAdvisoryBoard;
using ErrorBudgetFeature = NexTraceOne.Governance.Application.Features.EvaluateErrorBudgetGate.EvaluateErrorBudgetGate;
using FinOpsFeature = NexTraceOne.Governance.Application.Features.EvaluateFinOpsBudgetGate.EvaluateFinOpsBudgetGate;
using ComplianceFeature = NexTraceOne.Governance.Application.Features.EvaluateComplianceRemediationGate.EvaluateComplianceRemediationGate;

namespace NexTraceOne.Governance.API.Endpoints;

/// <summary>
/// Endpoints de governança avançada — Four Eyes Principle e Change Advisory Board.
/// Implementam gates de governança configuráveis via parametrização.
/// </summary>
public sealed class GovernanceGatesEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/governance/gates");

        // Four Eyes Principle evaluation
        group.MapGet("/four-eyes", async (
            string actionCode,
            string requestedBy,
            string? approvedBy,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new FourEyesFeature.Query(actionCode, requestedBy, approvedBy);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:gates:read");

        // Change Advisory Board evaluation
        group.MapGet("/cab", async (
            string serviceName,
            string environment,
            string criticality,
            string blastRadius,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new CabFeature.Query(serviceName, environment, criticality, blastRadius);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:gates:read");

        // Error Budget gate evaluation
        group.MapGet("/error-budget", async (
            string serviceName,
            string environment,
            decimal errorBudgetRemainingPct,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new ErrorBudgetFeature.Query(serviceName, environment, errorBudgetRemainingPct);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:gates:read");

        // FinOps budget gate evaluation
        group.MapGet("/finops-budget", async (
            string serviceName,
            string teamName,
            decimal currentSpendPct,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new FinOpsFeature.Query(serviceName, teamName, currentSpendPct);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:gates:read");

        // Compliance auto-remediation gate evaluation
        group.MapGet("/compliance-remediation", async (
            string violationType,
            string serviceName,
            string severity,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new ComplianceFeature.Query(violationType, serviceName, severity);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:gates:read");
    }
}
