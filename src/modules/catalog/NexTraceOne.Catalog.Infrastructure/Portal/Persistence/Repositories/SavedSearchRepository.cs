using Microsoft.EntityFrameworkCore;
using NexTraceOne.DeveloperPortal.Application.Abstractions;
using NexTraceOne.DeveloperPortal.Domain.Entities;

namespace NexTraceOne.DeveloperPortal.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de pesquisas salvas, implementando persistência via EF Core.
/// Suporta consultas por utilizador para gestão de pesquisas favoritas do catálogo.
/// </summary>
internal sealed class SavedSearchRepository(DeveloperPortalDbContext context) : ISavedSearchRepository
{
    /// <summary>Busca pesquisa salva por identificador único.</summary>
    public async Task<SavedSearch?> GetByIdAsync(SavedSearchId id, CancellationToken ct = default)
        => await context.SavedSearches.SingleOrDefaultAsync(s => s.Id == id, ct);

    /// <summary>Lista todas as pesquisas salvas de um utilizador, ordenadas pela mais recente.</summary>
    public async Task<IReadOnlyList<SavedSearch>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => await context.SavedSearches
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LastUsedAt)
            .ToListAsync(ct);

    /// <summary>Adiciona nova pesquisa salva ao contexto.</summary>
    public void Add(SavedSearch savedSearch)
        => context.SavedSearches.Add(savedSearch);

    /// <summary>Marca pesquisa salva como modificada no contexto.</summary>
    public void Update(SavedSearch savedSearch)
        => context.SavedSearches.Update(savedSearch);

    /// <summary>Remove pesquisa salva do contexto.</summary>
    public void Remove(SavedSearch savedSearch)
        => context.SavedSearches.Remove(savedSearch);
}
