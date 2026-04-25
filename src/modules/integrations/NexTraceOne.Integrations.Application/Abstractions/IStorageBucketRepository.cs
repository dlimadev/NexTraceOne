using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Integrations.Application.Abstractions;

/// <summary>
/// Repositório de buckets de storage de telemetria por tenant.
/// </summary>
public interface IStorageBucketRepository
{
    /// <summary>Lista buckets por tenant, ordenados por Priority ascendente.</summary>
    Task<(IReadOnlyList<StorageBucket> Items, int TotalCount)> ListAsync(
        bool? isEnabled,
        int page,
        int pageSize,
        CancellationToken ct);

    /// <summary>Lista buckets activos por tenant, ordenados por Priority (para o router).</summary>
    Task<IReadOnlyList<StorageBucket>> ListEnabledOrderedAsync(CancellationToken ct);

    /// <summary>Obtém um bucket pelo identificador.</summary>
    Task<StorageBucket?> GetByIdAsync(StorageBucketId id, CancellationToken ct);

    /// <summary>Adiciona um novo bucket.</summary>
    Task AddAsync(StorageBucket bucket, CancellationToken ct);

    /// <summary>Actualiza um bucket existente.</summary>
    Task UpdateAsync(StorageBucket bucket, CancellationToken ct);

    /// <summary>Remove um bucket.</summary>
    Task DeleteAsync(StorageBucket bucket, CancellationToken ct);
}
