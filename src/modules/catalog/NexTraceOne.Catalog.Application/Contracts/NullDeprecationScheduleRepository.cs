using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts;

/// <summary>
/// Implementação null (honest-null) de IDeprecationScheduleRepository.
/// Retorna null para consultas e ignora upserts.
/// Wave AV.3 — ScheduleContractDeprecation.
/// </summary>
public sealed class NullDeprecationScheduleRepository : IDeprecationScheduleRepository
{
    public Task<IDeprecationScheduleRepository.DeprecationScheduleRecord?> GetByContractIdAsync(
        Guid contractId, string tenantId, CancellationToken ct)
        => Task.FromResult<IDeprecationScheduleRepository.DeprecationScheduleRecord?>(null);

    public Task UpsertAsync(
        IDeprecationScheduleRepository.DeprecationScheduleRecord record, CancellationToken ct)
        => Task.CompletedTask;
}
