using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts;

/// <summary>
/// Implementação null (honest-null) de IDeprecationScheduleRepository.
/// Retorna null para consultas e ignora upserts.
/// Wave AV.3 — ScheduleContractDeprecation.
/// </summary>
public sealed class NullDeprecationScheduleRepository : IDeprecationScheduleRepository
{
    public Task<DeprecationScheduleRecord?> GetByContractIdAsync(
        Guid contractId, string tenantId, CancellationToken ct)
        => Task.FromResult<DeprecationScheduleRecord?>(null);

    public Task UpsertAsync(
        DeprecationScheduleRecord record, CancellationToken ct)
        => Task.CompletedTask;
}
