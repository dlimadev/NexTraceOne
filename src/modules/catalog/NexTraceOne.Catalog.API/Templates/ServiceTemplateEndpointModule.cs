using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using NexTraceOne.Catalog.Application.Templates.Features.ActivateServiceTemplate;
using NexTraceOne.Catalog.Application.Templates.Features.CreateServiceTemplate;
using NexTraceOne.Catalog.Application.Templates.Features.DeactivateServiceTemplate;
using NexTraceOne.Catalog.Application.Templates.Features.GetServiceTemplate;
using NexTraceOne.Catalog.Application.Templates.Features.ListServiceTemplates;
using NexTraceOne.Catalog.Application.Templates.Features.ScaffoldServiceFromTemplate;
using NexTraceOne.Catalog.Application.Templates.Features.UpdateServiceTemplate;
using NexTraceOne.Catalog.Domain.Templates.Enums;

namespace NexTraceOne.Catalog.API.Templates;

/// <summary>
/// Endpoints de Service Templates &amp; Scaffolding (Phase 3.1 do roadmap).
///
/// Fornece:
///   - CRUD de templates de serviço governados
///   - Scaffolding de novo serviço a partir de template
///
/// Política de autorização:
///   - Leitura: "catalog:templates:read"
///   - Criação/gestão: "catalog:templates:write"
///   - Scaffolding: "catalog:templates:scaffold"
/// </summary>
public sealed class ServiceTemplateEndpointModule
{
    /// <summary>Regista os endpoints de templates no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/catalog/templates")
            .WithTags("Service Templates")
            .RequireRateLimiting("standard");

        // ── POST /api/v1/catalog/templates — Criação de template ──
        group.MapPost("/", async (
            CreateServiceTemplate.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(r => $"/api/v1/catalog/templates/{r.TemplateId}", localizer);
        })
        .RequirePermission("catalog:templates:write")
        .WithName("CreateServiceTemplate")
        .WithSummary("Create a new governed service template for developer scaffolding");

        // ── GET /api/v1/catalog/templates — Listagem de templates ──
        group.MapGet("/", async (
            ISender sender,
            IErrorLocalizer localizer,
            bool? isActive,
            TemplateServiceType? serviceType,
            TemplateLanguage? language,
            string? search,
            Guid? tenantId,
            CancellationToken cancellationToken) =>
        {
            var query = new ListServiceTemplates.Query(isActive, serviceType, language, search, tenantId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("catalog:templates:read")
        .WithName("ListServiceTemplates")
        .WithSummary("List service templates with optional filters");

        // ── GET /api/v1/catalog/templates/{id} — Detalhe por id ──
        group.MapGet("/{id:guid}", async (
            Guid id,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetServiceTemplate.Query(id, null);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("catalog:templates:read")
        .WithName("GetServiceTemplate")
        .WithSummary("Get service template detail by ID");

        // ── GET /api/v1/catalog/templates/slug/{slug} — Detalhe por slug ──
        group.MapGet("/slug/{slug}", async (
            string slug,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetServiceTemplate.Query(null, slug);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("catalog:templates:read")
        .WithName("GetServiceTemplateBySlug")
        .WithSummary("Get service template detail by slug");

        // ── POST /api/v1/catalog/templates/{id}/scaffold — Scaffolding ──
        group.MapPost("/{id:guid}/scaffold", async (
            Guid id,
            ScaffoldRequest request,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new ScaffoldServiceFromTemplate.Command(
                TemplateId: id,
                TemplateSlug: null,
                ServiceName: request.ServiceName,
                TeamName: request.TeamName,
                Domain: request.Domain,
                RepositoryUrl: request.RepositoryUrl,
                ExtraVariables: request.ExtraVariables);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("catalog:templates:scaffold")
        .WithName("ScaffoldServiceFromTemplate")
        .WithSummary("Generate scaffolding plan for a new service based on a governed template");

        // ── POST /api/v1/catalog/templates/slug/{slug}/scaffold — Scaffolding por slug ──
        group.MapPost("/slug/{slug}/scaffold", async (
            string slug,
            ScaffoldRequest request,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new ScaffoldServiceFromTemplate.Command(
                TemplateId: null,
                TemplateSlug: slug,
                ServiceName: request.ServiceName,
                TeamName: request.TeamName,
                Domain: request.Domain,
                RepositoryUrl: request.RepositoryUrl,
                ExtraVariables: request.ExtraVariables);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("catalog:templates:scaffold")
        .WithName("ScaffoldServiceFromTemplateBySlug")
        .WithSummary("Generate scaffolding plan using template slug");

        // ── PUT /api/v1/catalog/templates/{id} — Atualização de template ──
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateServiceTemplateRequest request,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateServiceTemplate.Command(
                TemplateId: id,
                DisplayName: request.DisplayName,
                Description: request.Description,
                Version: request.Version,
                DefaultDomain: request.DefaultDomain,
                DefaultTeam: request.DefaultTeam,
                Tags: request.Tags,
                GovernancePolicyIds: request.GovernancePolicyIds,
                BaseContractSpec: request.BaseContractSpec,
                ScaffoldingManifestJson: request.ScaffoldingManifestJson,
                RepositoryTemplateUrl: request.RepositoryTemplateUrl,
                RepositoryTemplateBranch: request.RepositoryTemplateBranch);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("catalog:templates:write")
        .WithName("UpdateServiceTemplate")
        .WithSummary("Update an existing service template metadata and scaffolding manifest");

        // ── POST /api/v1/catalog/templates/{id}/activate — Ativar template ──
        group.MapPost("/{id:guid}/activate", async (
            Guid id,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new ActivateServiceTemplate.Command(id);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("catalog:templates:write")
        .WithName("ActivateServiceTemplate")
        .WithSummary("Activate a service template to make it available for scaffolding");

        // ── POST /api/v1/catalog/templates/{id}/deactivate — Desativar template ──
        group.MapPost("/{id:guid}/deactivate", async (
            Guid id,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new DeactivateServiceTemplate.Command(id);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("catalog:templates:write")
        .WithName("DeactivateServiceTemplate")
        .WithSummary("Deactivate a service template to prevent new scaffolding");
    }
}

/// <summary>Body do pedido de scaffolding.</summary>
internal sealed record ScaffoldRequest(
    string ServiceName,
    string? TeamName = null,
    string? Domain = null,
    string? RepositoryUrl = null,
    IDictionary<string, string>? ExtraVariables = null);

/// <summary>Body do pedido de atualização de template.</summary>
internal sealed record UpdateServiceTemplateRequest(
    string DisplayName,
    string Description,
    string Version,
    string DefaultDomain,
    string DefaultTeam,
    IReadOnlyList<string>? Tags = null,
    IReadOnlyList<Guid>? GovernancePolicyIds = null,
    string? BaseContractSpec = null,
    string? ScaffoldingManifestJson = null,
    string? RepositoryTemplateUrl = null,
    string? RepositoryTemplateBranch = null);
