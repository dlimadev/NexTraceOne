using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using ListPacksFeature = NexTraceOne.Governance.Application.Features.ListGovernancePacks.ListGovernancePacks;
using GetPackFeature = NexTraceOne.Governance.Application.Features.GetGovernancePack.GetGovernancePack;
using CreatePackFeature = NexTraceOne.Governance.Application.Features.CreateGovernancePack.CreateGovernancePack;
using UpdatePackFeature = NexTraceOne.Governance.Application.Features.UpdateGovernancePack.UpdateGovernancePack;
using ListVersionsFeature = NexTraceOne.Governance.Application.Features.ListPackVersions.ListPackVersions;
using CreateVersionFeature = NexTraceOne.Governance.Application.Features.CreatePackVersion.CreatePackVersion;
using GetApplicabilityFeature = NexTraceOne.Governance.Application.Features.GetPackApplicability.GetPackApplicability;
using ApplyPackFeature = NexTraceOne.Governance.Application.Features.ApplyGovernancePack.ApplyGovernancePack;
using GetCoverageFeature = NexTraceOne.Governance.Application.Features.GetPackCoverage.GetPackCoverage;

namespace NexTraceOne.Governance.API.Endpoints;

/// <summary>
/// Endpoints de Governance Packs — gestão de pacotes de governança enterprise.
/// Disponibiliza CRUD, versionamento, aplicação e cobertura de packs.
/// </summary>
public sealed class GovernancePacksEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/governance/packs");

        // ── Listar governance packs ──
        group.MapGet("/", async (
            string? category,
            string? status,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new ListPacksFeature.Query(category, status);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:packs:read");

        // ── Detalhe de um governance pack ──
        group.MapGet("/{packId}", async (
            string packId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPackFeature.Query(packId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:packs:read");

        // ── Criar novo governance pack ──
        group.MapPost("/", async (
            CreatePackFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(r => $"/api/v1/governance/packs/{r.PackId}", localizer);
        }).RequirePermission("governance:packs:write");

        // ── Atualizar governance pack existente ──
        group.MapPatch("/{packId}", async (
            string packId,
            UpdatePackFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var cmd = command with { PackId = packId };
            var result = await sender.Send(cmd, cancellationToken);
            if (result.IsSuccess)
                return Results.NoContent();
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:packs:write");

        // ── Listar versões de um governance pack ──
        group.MapGet("/{packId}/versions", async (
            string packId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new ListVersionsFeature.Query(packId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:packs:read");

        // ── Criar nova versão de um governance pack ──
        group.MapPost("/{packId}/versions", async (
            string packId,
            CreateVersionFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var cmd = command with { PackId = packId };
            var result = await sender.Send(cmd, cancellationToken);
            return result.ToCreatedResult(r => $"/api/v1/governance/packs/{r.PackId}/versions", localizer);
        }).RequirePermission("governance:packs:write");

        // ── Consultar aplicabilidade de um governance pack ──
        group.MapGet("/{packId}/applicability", async (
            string packId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetApplicabilityFeature.Query(packId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:packs:read");

        // ── Aplicar governance pack a um escopo ──
        group.MapPost("/{packId}/apply", async (
            string packId,
            ApplyPackFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var cmd = command with { PackId = packId };
            var result = await sender.Send(cmd, cancellationToken);
            return result.ToCreatedResult(r => $"/api/v1/governance/packs/{r.PackId}/apply", localizer);
        }).RequirePermission("governance:packs:write");

        // ── Consultar cobertura de um governance pack ──
        group.MapGet("/{packId}/coverage", async (
            string packId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCoverageFeature.Query(packId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:packs:read");
    }
}
