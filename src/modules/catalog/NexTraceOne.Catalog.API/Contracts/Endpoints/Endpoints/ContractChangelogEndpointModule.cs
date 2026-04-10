using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using GenerateChangelogFeature = NexTraceOne.Catalog.Application.Contracts.Features.GenerateContractChangelog.GenerateContractChangelog;
using ApproveChangelogFeature = NexTraceOne.Catalog.Application.Contracts.Features.ApproveContractChangelog.ApproveContractChangelog;
using ListChangelogsFeature = NexTraceOne.Catalog.Application.Contracts.Features.ListContractChangelogs.ListContractChangelogs;
using GetChangelogFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetContractChangelog.GetContractChangelog;

namespace NexTraceOne.Catalog.API.Contracts.Endpoints.Endpoints;

/// <summary>
/// Registra os endpoints Minimal API de changelog de contrato.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
///
/// Endpoints:
/// - POST /api/v1/contracts/changelogs                    — gera entrada de changelog
/// - POST /api/v1/contracts/changelogs/{id}/approve       — aprova formalmente um changelog
/// - GET  /api/v1/contracts/changelogs                    — lista changelogs com filtros
/// - GET  /api/v1/contracts/changelogs/{id}               — obtém detalhe completo de um changelog
///
/// Política de autorização:
/// - Geração e aprovação exigem "contracts:write".
/// - Consultas exigem "contracts:read".
/// </summary>
public sealed class ContractChangelogEndpointModule
{
    /// <summary>Registra os endpoints de changelog de contrato no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/contracts/changelogs");

        // POST /api/v1/contracts/changelogs
        group.MapPost("/", async (
            GenerateChangelogFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:write");

        // POST /api/v1/contracts/changelogs/{changelogId}/approve
        group.MapPost("/{changelogId:guid}/approve", async (
            Guid changelogId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ApproveChangelogFeature.Command(changelogId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:write");

        // GET /api/v1/contracts/changelogs?apiAssetId=&pendingApprovalOnly=
        group.MapGet("/", async (
            string? apiAssetId,
            bool? pendingApprovalOnly,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListChangelogsFeature.Query(apiAssetId, pendingApprovalOnly ?? false),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        // GET /api/v1/contracts/changelogs/{changelogId}
        group.MapGet("/{changelogId:guid}", async (
            Guid changelogId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetChangelogFeature.Query(changelogId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");
    }
}
