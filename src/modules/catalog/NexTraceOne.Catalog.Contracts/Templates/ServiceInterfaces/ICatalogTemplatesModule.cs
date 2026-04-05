namespace NexTraceOne.Catalog.Contracts.Templates.ServiceInterfaces;

/// <summary>
/// Interface de comunicação entre módulos para acesso a templates de serviço.
/// Usada pelo módulo AIKnowledge para resolver templates durante a geração de scaffolding com IA.
///
/// Regra: o AIKnowledge nunca acede diretamente ao DbContext do Catalog.
/// Toda comunicação é feita através desta interface e da sua implementação no Catalog.Infrastructure.
/// </summary>
public interface ICatalogTemplatesModule
{
    /// <summary>
    /// Retorna o resumo de um template pelo seu ID.
    /// Retorna null se o template não existir ou estiver desativado.
    /// </summary>
    Task<ServiceTemplateSummary?> GetActiveTemplateAsync(Guid templateId, CancellationToken ct = default);

    /// <summary>
    /// Retorna o resumo de um template pelo seu slug.
    /// Retorna null se o template não existir ou estiver desativado.
    /// </summary>
    Task<ServiceTemplateSummary?> GetActiveTemplateBySlugAsync(string slug, CancellationToken ct = default);
}

/// <summary>
/// Resumo de template exposto pelo módulo Catalog para consumo do AIKnowledge.
/// Contém apenas os campos necessários para a geração de scaffolding com IA.
/// </summary>
public sealed record ServiceTemplateSummary(
    Guid TemplateId,
    string Slug,
    string DisplayName,
    string Description,
    string Version,
    string ServiceType,
    string Language,
    string DefaultDomain,
    string DefaultTeam,
    IReadOnlyList<string> Tags,
    string? BaseContractSpec,
    string? ScaffoldingManifestJson,
    string? RepositoryTemplateUrl);
