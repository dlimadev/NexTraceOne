namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

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
