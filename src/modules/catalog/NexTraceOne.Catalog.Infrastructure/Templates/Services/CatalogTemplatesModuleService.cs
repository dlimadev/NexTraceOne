using NexTraceOne.Catalog.Application.Templates.Abstractions;
using NexTraceOne.Catalog.Contracts.Templates.ServiceInterfaces;

namespace NexTraceOne.Catalog.Infrastructure.Templates.Services;

/// <summary>
/// Implementação do contrato inter-módulo ICatalogTemplatesModule.
/// Permite ao módulo AIKnowledge resolver templates activos sem aceder directamente
/// ao DbContext do Catalog — respeitando a fronteira de bounded context.
///
/// Regra: o AIKnowledge nunca acede ao DbContext do Catalog.
/// Toda comunicação é feita via esta implementação registada em DI.
/// </summary>
internal sealed class CatalogTemplatesModuleService(
    IServiceTemplateRepository repository) : ICatalogTemplatesModule
{
    /// <inheritdoc />
    public async Task<ServiceTemplateSummary?> GetActiveTemplateAsync(
        Guid templateId,
        CancellationToken ct = default)
    {
        var template = await repository.GetByIdAsync(templateId, ct);
        if (template is null || !template.IsActive)
            return null;

        return MapToSummary(template);
    }

    /// <inheritdoc />
    public async Task<ServiceTemplateSummary?> GetActiveTemplateBySlugAsync(
        string slug,
        CancellationToken ct = default)
    {
        var template = await repository.GetBySlugAsync(slug, ct);
        if (template is null || !template.IsActive)
            return null;

        return MapToSummary(template);
    }

    private static ServiceTemplateSummary MapToSummary(
        NexTraceOne.Catalog.Domain.Templates.Entities.ServiceTemplate template)
        => new(
            TemplateId: template.Id.Value,
            Slug: template.Slug,
            DisplayName: template.DisplayName,
            Description: template.Description,
            Version: template.Version,
            ServiceType: template.ServiceType.ToString(),
            Language: template.Language.ToString(),
            DefaultDomain: template.DefaultDomain,
            DefaultTeam: template.DefaultTeam,
            Tags: template.Tags,
            BaseContractSpec: template.BaseContractSpec,
            ScaffoldingManifestJson: template.ScaffoldingManifestJson,
            RepositoryTemplateUrl: template.RepositoryTemplateUrl);
}
