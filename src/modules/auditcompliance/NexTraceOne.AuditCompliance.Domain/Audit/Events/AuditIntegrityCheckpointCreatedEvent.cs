using NexTraceOne.BuildingBlocks.Core.Events;

namespace NexTraceOne.AuditCompliance.Domain.Audit.Events;

/// <summary>
/// Evento emitido quando um checkpoint de integridade da trilha de auditoria é criado.
/// Consumidores típicos: monitoramento de compliance, alertas de segurança.
/// </summary>
public sealed record AuditIntegrityCheckpointCreatedEvent(
    Guid CheckpointId,
    DateTimeOffset PeriodFrom,
    DateTimeOffset PeriodTo,
    string Hash,
    DateTimeOffset CreatedAt) : IntegrationEventBase("AuditCompliance");
