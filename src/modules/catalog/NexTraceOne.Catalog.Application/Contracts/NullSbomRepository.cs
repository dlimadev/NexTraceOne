using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts;

/// <summary>
/// Implementação null (honest-null) de ISbomRepository.
/// Não persiste dados — serve como bridge sem infra real.
/// Wave AO.1 — IngestSbomRecord.
/// </summary>
public sealed class NullSbomRepository : ISbomRepository
{
    public Task AddAsync(SbomRecord record, CancellationToken ct) => Task.CompletedTask;

    public Task<SbomRecord?> GetLatestAsync(string serviceId, string tenantId, CancellationToken ct)
        => Task.FromResult<SbomRecord?>(null);

    public Task<IReadOnlyList<SbomRecord>> ListByTenantAsync(string tenantId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<SbomRecord>>([]);
}
