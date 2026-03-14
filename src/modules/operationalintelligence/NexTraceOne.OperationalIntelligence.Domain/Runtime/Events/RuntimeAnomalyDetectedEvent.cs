using NexTraceOne.BuildingBlocks.Core;

namespace NexTraceOne.RuntimeIntelligence.Domain.Events;

/// <summary>
/// Evento emitido quando uma anomalia de runtime é detectada.
/// Consumidores típicos: ChangeIntelligence (blast radius), Audit, Notification.
/// </summary>
public sealed record RuntimeAnomalyDetectedEvent(
    Guid AnomalyId,
    string ServiceName,
    string AnomalyType,
    double Severity,
    DateTimeOffset DetectedAt) : IntegrationEventBase("OperationalIntelligence");
