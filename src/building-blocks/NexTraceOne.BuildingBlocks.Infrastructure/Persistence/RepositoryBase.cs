using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Implementação base de repositório usando EF Core.
/// CRUD completo + Specification evaluation. Módulos só implementam métodos de negócio.
/// </summary>
public abstract class RepositoryBase<TEntity, TId>(DbContext context)
    where TEntity : AggregateRoot<TId>
    where TId     : ITypedId
{
    protected readonly DbSet<TEntity> DbSet = context.Set<TEntity>();

    public async Task<TEntity?> GetByIdAsync(TId id, CancellationToken ct = default)
        => await DbSet.FindAsync([id], ct);

    public async Task<TEntity> GetByIdOrThrowAsync(TId id, CancellationToken ct = default)
    {
        // TODO: Implementar com lançamento de NexTraceNotFoundException
        throw new NotImplementedException();
    }

    public Task<bool> ExistsAsync(TId id, CancellationToken ct = default)
        => DbSet.AnyAsync(e => e.Id.Equals(id), ct);

    public void Add(TEntity entity) => DbSet.Add(entity);
    public void Update(TEntity entity) => DbSet.Update(entity);
    public void Remove(TEntity entity) => DbSet.Remove(entity);
}
