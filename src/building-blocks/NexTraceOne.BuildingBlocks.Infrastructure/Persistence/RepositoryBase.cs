using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Implementação base de repositório usando EF Core.
/// CRUD completo + Specification evaluation. Módulos só implementam métodos de negócio.
/// </summary>
public abstract class RepositoryBase<TEntity, TId>(DbContext context)
    where TEntity : Entity<TId>
    where TId     : ITypedId
{
    protected readonly DbContext Context = context;
    protected readonly DbSet<TEntity> DbSet = context.Set<TEntity>();

    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken ct = default)
        => await DbSet.FindAsync([id.Value], ct);

    public async Task<TEntity> GetByIdOrThrowAsync(TId id, CancellationToken ct = default)
    {
        var entity = await GetByIdAsync(id, ct);
        return entity ?? throw new KeyNotFoundException($"Entity '{typeof(TEntity).Name}' with id '{id}' was not found.");
    }

    public Task<bool> ExistsAsync(TId id, CancellationToken ct = default)
        => DbSet.AnyAsync(e => e.Id.Equals(id), ct);

    public void Add(TEntity entity) => DbSet.Add(entity);
    public void Update(TEntity entity) => DbSet.Update(entity);
    public void Remove(TEntity entity) => DbSet.Remove(entity);
}
