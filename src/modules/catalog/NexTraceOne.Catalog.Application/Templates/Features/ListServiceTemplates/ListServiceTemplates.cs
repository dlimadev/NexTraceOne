using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Templates.Abstractions;
using NexTraceOne.Catalog.Domain.Templates.Enums;

namespace NexTraceOne.Catalog.Application.Templates.Features.ListServiceTemplates;

/// <summary>
/// Feature: ListServiceTemplates — lista templates de serviço com filtros.
/// Suporta filtragem por estado, tipo, linguagem, pesquisa textual e tenant.
/// Persona primária: Developer, Platform Admin.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ListServiceTemplates
{
    /// <summary>Query para listar templates com filtros opcionais.</summary>
    public sealed record Query(
        bool? IsActive = null,
        TemplateServiceType? ServiceType = null,
        TemplateLanguage? Language = null,
        string? Search = null,
        Guid? TenantId = null) : IQuery<Response>;

    /// <summary>Valida os filtros da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Search)
                .MaximumLength(200)
                .When(x => x.Search is not null);
        }
    }

    /// <summary>Handler que filtra e devolve a lista de templates.</summary>
    public sealed class Handler(IServiceTemplateRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var templates = await repository.ListAsync(
                request.IsActive, request.ServiceType, request.Language,
                request.Search, request.TenantId, cancellationToken);

            var items = templates
                .Select(t => new TemplateSummary(
                    TemplateId: t.Id.Value,
                    Slug: t.Slug,
                    DisplayName: t.DisplayName,
                    Description: t.Description,
                    Version: t.Version,
                    ServiceType: t.ServiceType,
                    Language: t.Language,
                    DefaultDomain: t.DefaultDomain,
                    DefaultTeam: t.DefaultTeam,
                    Tags: t.Tags,
                    IsActive: t.IsActive,
                    UsageCount: t.UsageCount,
                    HasBaseContract: t.BaseContractSpec is not null,
                    HasScaffoldingManifest: t.ScaffoldingManifestJson is not null))
                .ToList()
                .AsReadOnly();

            return Result<Response>.Success(new Response(items, items.Count));
        }
    }

    /// <summary>Sumário de um template para listagem.</summary>
    public sealed record TemplateSummary(
        Guid TemplateId,
        string Slug,
        string DisplayName,
        string Description,
        string Version,
        TemplateServiceType ServiceType,
        TemplateLanguage Language,
        string DefaultDomain,
        string DefaultTeam,
        IReadOnlyList<string> Tags,
        bool IsActive,
        int UsageCount,
        bool HasBaseContract,
        bool HasScaffoldingManifest);

    /// <summary>Resposta com a lista de templates e total.</summary>
    public sealed record Response(IReadOnlyList<TemplateSummary> Items, int Total);
}
