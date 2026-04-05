using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstração do repositório de versões de entidades canónicas.
/// </summary>
public interface ICanonicalEntityVersionRepository
{
    /// <summary>Lista todas as versões de uma entidade canónica, ordenadas por data de publicação.</summary>
    Task<IReadOnlyList<CanonicalEntityVersion>> ListByEntityIdAsync(
        CanonicalEntityId entityId,
        CancellationToken cancellationToken = default);

    /// <summary>Busca uma versão específica de uma entidade canónica.</summary>
    Task<CanonicalEntityVersion?> GetByVersionAsync(
        CanonicalEntityId entityId,
        string version,
        CancellationToken cancellationToken = default);

    /// <summary>Adiciona uma nova versão de entidade canónica.</summary>
    void Add(CanonicalEntityVersion version);
}
