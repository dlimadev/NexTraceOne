using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using ListDomainsFeature = NexTraceOne.Governance.Application.Features.ListDomains.ListDomains;
using GetDomainDetailFeature = NexTraceOne.Governance.Application.Features.GetDomainDetail.GetDomainDetail;
using CreateDomainFeature = NexTraceOne.Governance.Application.Features.CreateDomain.CreateDomain;
using UpdateDomainFeature = NexTraceOne.Governance.Application.Features.UpdateDomain.UpdateDomain;
using GetDomainGovernanceSummaryFeature = NexTraceOne.Governance.Application.Features.GetDomainGovernanceSummary.GetDomainGovernanceSummary;
using GetCrossDomainDependenciesFeature = NexTraceOne.Governance.Application.Features.GetCrossDomainDependencies.GetCrossDomainDependencies;

namespace NexTraceOne.Governance.API.Endpoints;

/// <summary>
/// Endpoints de gestão de domínios no módulo Governance.
/// Disponibiliza CRUD, resumo de governança e dependências cross-domain.
/// Domínios representam áreas de negócio e agrupam equipas e serviços no NexTraceOne.
/// </summary>
public sealed class DomainEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/domains");

        // ── Listar todos os domínios ──
        group.MapGet("/", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new ListDomainsFeature.Query();
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:domains:read");

        // ── Detalhe de um domínio ──
        group.MapGet("/{domainId}", async (
            string domainId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetDomainDetailFeature.Query(domainId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:domains:read");

        // ── Criar novo domínio ──
        group.MapPost("/", async (
            CreateDomainFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            if (result.IsSuccess)
                return Results.Created($"/api/v1/domains/{result.Value.DomainId}", result.Value);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:domains:write");

        // ── Atualizar domínio existente ──
        group.MapPatch("/{domainId}", async (
            string domainId,
            UpdateDomainFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var cmd = command with { DomainId = domainId };
            var result = await sender.Send(cmd, cancellationToken);
            if (result.IsSuccess)
                return Results.NoContent();
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:domains:write");

        // ── Resumo de governança do domínio ──
        group.MapGet("/{domainId}/governance-summary", async (
            string domainId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetDomainGovernanceSummaryFeature.Query(domainId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:domains:read");

        // ── Dependências cross-domain do domínio ──
        group.MapGet("/{domainId}/dependencies/cross-domain", async (
            string domainId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCrossDomainDependenciesFeature.Query(domainId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("governance:domains:read");
    }
}
