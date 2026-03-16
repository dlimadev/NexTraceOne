using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using ListTeamsFeature = NexTraceOne.Governance.Application.Features.ListTeams.ListTeams;
using GetTeamDetailFeature = NexTraceOne.Governance.Application.Features.GetTeamDetail.GetTeamDetail;
using CreateTeamFeature = NexTraceOne.Governance.Application.Features.CreateTeam.CreateTeam;
using UpdateTeamFeature = NexTraceOne.Governance.Application.Features.UpdateTeam.UpdateTeam;
using GetTeamGovernanceSummaryFeature = NexTraceOne.Governance.Application.Features.GetTeamGovernanceSummary.GetTeamGovernanceSummary;
using GetCrossTeamDependenciesFeature = NexTraceOne.Governance.Application.Features.GetCrossTeamDependencies.GetCrossTeamDependencies;

namespace NexTraceOne.Governance.API.Endpoints;

/// <summary>
/// Endpoints de gestão de equipas no módulo Governance.
/// Disponibiliza CRUD, resumo de governança e dependências cross-team.
/// Equipas são entidades centrais de ownership no NexTraceOne.
/// </summary>
public sealed class TeamEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/teams");

        // ── Listar todas as equipas ──
        group.MapGet("/", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new ListTeamsFeature.Query();
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:teams:read");

        // ── Detalhe de uma equipa ──
        group.MapGet("/{teamId}", async (
            string teamId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetTeamDetailFeature.Query(teamId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:teams:read");

        // ── Criar nova equipa ──
        group.MapPost("/", async (
            CreateTeamFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            if (result.IsSuccess)
                return Results.Created($"/api/v1/teams/{result.Value.TeamId}", result.Value);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:teams:write");

        // ── Atualizar equipa existente ──
        group.MapPatch("/{teamId}", async (
            string teamId,
            UpdateTeamFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var cmd = command with { TeamId = teamId };
            var result = await sender.Send(cmd, cancellationToken);
            if (result.IsSuccess)
                return Results.NoContent();
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:teams:write");

        // ── Resumo de governança da equipa ──
        group.MapGet("/{teamId}/governance-summary", async (
            string teamId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetTeamGovernanceSummaryFeature.Query(teamId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:teams:read");

        // ── Dependências cross-team da equipa ──
        group.MapGet("/{teamId}/dependencies/cross-team", async (
            string teamId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCrossTeamDependenciesFeature.Query(teamId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:teams:read");
    }
}
