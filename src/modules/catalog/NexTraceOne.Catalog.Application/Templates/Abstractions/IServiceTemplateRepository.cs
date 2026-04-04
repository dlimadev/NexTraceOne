using NexTraceOne.Catalog.Domain.Templates.Entities;
using NexTraceOne.Catalog.Domain.Templates.Enums;

namespace NexTraceOne.Catalog.Application.Templates.Abstractions;

/// <summary>
/// Contrato de repositório para ServiceTemplate.
/// Separação de preocupações: a Application Layer depende desta interface,
/// não de implementações concretas de infraestrutura.
/// </summary>
public interface IServiceTemplateRepository
{
    /// <summary>Retorna o template com o id indicado, ou null se não existir.</summary>
    Task<ServiceTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Retorna o template com o slug indicado, ou null se não existir.</summary>
    Task<ServiceTemplate?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna todos os templates, com filtros opcionais.
    /// </summary>
    Task<IReadOnlyList<ServiceTemplate>> ListAsync(
        bool? isActive,
        TemplateServiceType? serviceType,
        TemplateLanguage? language,
        string? search,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>Verifica se existe um template com o slug indicado.</summary>
    Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Persiste um novo template.</summary>
    Task AddAsync(ServiceTemplate template, CancellationToken cancellationToken = default);

    /// <summary>Atualiza um template existente.</summary>
    Task UpdateAsync(ServiceTemplate template, CancellationToken cancellationToken = default);
}
