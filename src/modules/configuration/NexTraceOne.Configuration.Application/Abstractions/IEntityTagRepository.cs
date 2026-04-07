using NexTraceOne.BuildingBlocks.Core.Tags;

namespace NexTraceOne.Configuration.Application.Abstractions;

/// <summary>Contrato do repositório de tags de entidades.</summary>
public interface IEntityTagRepository
{
    Task<EntityTag?> GetByIdAsync(EntityTagId id, string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<EntityTag>> ListByEntityAsync(string tenantId, string entityType, string entityId, CancellationToken cancellationToken);
    Task<IReadOnlyList<EntityTag>> ListByKeyAsync(string tenantId, string key, CancellationToken cancellationToken);
    Task AddAsync(EntityTag tag, CancellationToken cancellationToken);
    Task UpdateAsync(EntityTag tag, CancellationToken cancellationToken);
    Task DeleteAsync(EntityTagId id, CancellationToken cancellationToken);
}
