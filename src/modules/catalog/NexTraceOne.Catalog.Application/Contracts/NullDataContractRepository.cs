using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Entities;

namespace NexTraceOne.Catalog.Application.Contracts;

/// <summary>Wave AQ.1 — honest-null IDataContractRepository.</summary>
public sealed class NullDataContractRepository : IDataContractRepository
{
    public Task AddAsync(DataContractRecord record, CancellationToken ct) => Task.CompletedTask;

    public Task<IReadOnlyList<DataContractRecord>> ListByTenantAsync(string tenantId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<DataContractRecord>>([]);

    public Task<IReadOnlyList<DataContractRecord>> ListByTeamAsync(string tenantId, string teamId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<DataContractRecord>>([]);
}
