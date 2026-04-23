using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Entities;

namespace NexTraceOne.Catalog.Application.Contracts;

/// <summary>
/// Implementação null (honest-null) de IFeatureFlagRepository.
/// Não persiste dados — serve como bridge sem infra real.
/// Wave AS.1 — IngestFeatureFlagState.
/// </summary>
public sealed class NullFeatureFlagRepository : IFeatureFlagRepository
{
    public Task UpsertAsync(FeatureFlagRecord record, CancellationToken ct) => Task.CompletedTask;

    public Task<IReadOnlyList<FeatureFlagRecord>> ListByTenantAsync(string tenantId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<FeatureFlagRecord>>([]);

    public Task<IReadOnlyList<FeatureFlagRecord>> ListByServiceAsync(string serviceId, string tenantId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<FeatureFlagRecord>>([]);
}
