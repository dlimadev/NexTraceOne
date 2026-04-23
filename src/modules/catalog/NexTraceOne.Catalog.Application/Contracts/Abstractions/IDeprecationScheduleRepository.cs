namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Repositório de agendamentos de deprecação de contratos.
/// Por omissão satisfeita por <c>NullDeprecationScheduleRepository</c> (honest-null).
/// Wave AV.3 — ScheduleContractDeprecation.
/// </summary>
public interface IDeprecationScheduleRepository
{
    Task<DeprecationScheduleRecord?> GetByContractIdAsync(Guid contractId, string tenantId, CancellationToken ct);
    Task UpsertAsync(DeprecationScheduleRecord record, CancellationToken ct);

    /// <summary>Registo de agendamento de deprecação de um contrato.</summary>
    public sealed record DeprecationScheduleRecord(
        Guid Id,
        Guid ContractId,
        string TenantId,
        DateTimeOffset PlannedDeprecationDate,
        DateTimeOffset? PlannedSunsetDate,
        string? MigrationGuideUrl,
        Guid? SuccessorVersionId,
        string? NotificationDraftMessage,
        string ScheduledByUserId,
        string? Reason,
        DateTimeOffset ScheduledAt);
}
