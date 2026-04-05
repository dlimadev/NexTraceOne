using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstração do repositório de entidades canónicas.
/// </summary>
public interface ICanonicalEntityRepository
{
    /// <summary>Busca uma entidade canónica pelo seu identificador.</summary>
    Task<CanonicalEntity?> GetByIdAsync(CanonicalEntityId id, CancellationToken ct = default);

    /// <summary>
    /// Pesquisa entidades canónicas com filtros opcionais e paginação.
    /// Retorna os itens da página solicitada e o total de registros.
    /// </summary>
    Task<(IReadOnlyList<CanonicalEntity> Items, int TotalCount)> SearchAsync(
        string? searchTerm,
        string? domain,
        string? category,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Adiciona uma nova entidade canónica ao repositório.</summary>
    void Add(CanonicalEntity entity);
}
