using NexTraceOne.Catalog.Domain.Portal.Entities;

namespace NexTraceOne.Catalog.Application.Portal.Abstractions;

/// <summary>
/// Repositório de buscas salvas do módulo DeveloperPortal.
/// Permite que utilizadores guardem e reutilizem consultas frequentes no catálogo.
/// </summary>
public interface ISavedSearchRepository
{
    Task<SavedSearch?> GetByIdAsync(SavedSearchId id, CancellationToken ct = default);
    Task<IReadOnlyList<SavedSearch>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    void Add(SavedSearch savedSearch);
    void Update(SavedSearch savedSearch);
    void Remove(SavedSearch savedSearch);
}
