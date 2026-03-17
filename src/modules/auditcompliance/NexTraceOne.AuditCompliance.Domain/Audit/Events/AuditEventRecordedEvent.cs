using NexTraceOne.BuildingBlocks.Core.Events;

namespace NexTraceOne.AuditCompliance.Domain.Audit.Events;

/// <summary>
/// Evento emitido quando um evento de auditoria é registrado.
/// Consumidores típicos: integrações externas, exportação, dashboards.
/// </summary>
public sealed record AuditEventRecordedEvent(
    Guid AuditEventId,
    string EventType,
    string Actor,
    DateTimeOffset RecordedAt) : IntegrationEventBase("AuditCompliance");
