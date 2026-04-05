using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Repositório de entidades canónicas com pesquisa por filtros e paginação.
/// </summary>
internal sealed class CanonicalEntityRepository(ContractsDbContext context)
    : ICanonicalEntityRepository
{
    /// <inheritdoc />
    public async Task<CanonicalEntity?> GetByIdAsync(CanonicalEntityId id, CancellationToken ct = default)
        => await context.CanonicalEntities.SingleOrDefaultAsync(e => e.Id == id, ct);

    /// <inheritdoc />
    public async Task<(IReadOnlyList<CanonicalEntity> Items, int TotalCount)> SearchAsync(
        string? searchTerm,
        string? domain,
        string? category,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.CanonicalEntities.Where(e => !e.IsDeleted);

        if (!string.IsNullOrWhiteSpace(domain))
            query = query.Where(e => e.Domain == domain);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(e => e.Category == category);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(e =>
                EF.Functions.ILike(e.Name, $"%{term}%") ||
                EF.Functions.ILike(e.Description, $"%{term}%") ||
                EF.Functions.ILike(e.Owner, $"%{term}%"));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public void Add(CanonicalEntity entity) => context.CanonicalEntities.Add(entity);
}
