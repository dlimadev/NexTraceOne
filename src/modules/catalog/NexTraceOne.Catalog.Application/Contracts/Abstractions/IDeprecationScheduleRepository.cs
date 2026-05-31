using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Repositório de agendamentos de deprecação de contratos.
/// Implementação EF Core em Infrastructure/Contracts/Persistence/Repositories/EfDeprecationScheduleRepository.
/// Wave AV.3 — ScheduleContractDeprecation.
/// </summary>
public interface IDeprecationScheduleRepository
{
    Task<DeprecationScheduleRecord?> GetByContractIdAsync(Guid contractId, string tenantId, CancellationToken ct);
    Task UpsertAsync(DeprecationScheduleRecord record, CancellationToken ct);
}
