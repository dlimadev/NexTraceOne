using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using RegisterFeature = NexTraceOne.Governance.Application.Features.RegisterPolicyAsCode.RegisterPolicyAsCode;
using GetFeature = NexTraceOne.Governance.Application.Features.GetPolicyAsCode.GetPolicyAsCode;
using SimulateFeature = NexTraceOne.Governance.Application.Features.SimulatePolicyApplication.SimulatePolicyApplication;
using TransitionFeature = NexTraceOne.Governance.Application.Features.TransitionEnforcementMode.TransitionEnforcementMode;
using ExpireFeature = NexTraceOne.Governance.Application.Features.ExpireGovernanceWaivers.ExpireGovernanceWaivers;
using PreCommitFeature = NexTraceOne.Governance.Application.Features.RunPreCommitGovernanceCheck.RunPreCommitGovernanceCheck;

namespace NexTraceOne.Governance.API.Endpoints;

/// <summary>
/// Endpoints do Governance Policy Engine V2 — Policy as Code, simulação de impacto
/// e gestão de enforcement gradual.
/// </summary>
public sealed class PolicyAsCodeEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var policyGroup = app.MapGroup("/api/v1/governance/policy-as-code");

        // ── Registar nova política como código ──
        policyGroup.MapPost("/", async (
            RegisterFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(r => $"/api/v1/governance/policy-as-code/{r.Id}", localizer);
        }).RequirePermission("governance:policies:write");

        // ── Obter política como código pelo nome ──
        policyGroup.MapGet("/{policyName}", async (
            string policyName,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetFeature.Query(policyName), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:policies:read");

        // ── Simular aplicação de uma política ──
        policyGroup.MapPost("/{policyName}/simulate", async (
            string policyName,
            SimulateFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command with { PolicyName = policyName }, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:policies:simulate");

        // ── Fazer transição de enforcement mode ──
        policyGroup.MapPost("/{policyName}/transition", async (
            string policyName,
            TransitionFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command with { PolicyName = policyName }, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:policies:write");

        // ── Verificação pré-commit de governança (Phase 7.6) ──
        policyGroup.MapPost("/pre-commit-check", async (
            PreCommitFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:policies:read");

        // ── Expirar waivers vencidos (job de manutenção) ──
        var waiverGroup = app.MapGroup("/api/v1/governance/waivers");
        waiverGroup.MapPost("/expire", async (
            ExpireFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:waivers:admin");
    }
}
