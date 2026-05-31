using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Implementação em memória do repositório de deprecation schedules.
/// Usada exclusivamente em testes unitários.
/// </summary>
public sealed class TestDeprecationScheduleRepository : IDeprecationScheduleRepository
{
    private readonly Dictionary<(Guid ContractId, string TenantId), DeprecationScheduleRecord> _store = [];

    public Task<DeprecationScheduleRecord?> GetByContractIdAsync(Guid contractId, string tenantId, CancellationToken ct)
    {
        _store.TryGetValue((contractId, tenantId), out var record);
        return Task.FromResult(record);
    }

    public Task UpsertAsync(DeprecationScheduleRecord record, CancellationToken ct)
    {
        _store[(record.ContractId, record.TenantId)] = record;
        return Task.CompletedTask;
    }
}
