using NexTraceOne.BuildingBlocks.Core;

namespace NexTraceOne.CostIntelligence.Domain.Events;

/// <summary>
/// Evento emitido quando uma anomalia de custo operacional é detectada.
/// Consumidores típicos: ChangeIntelligence (correlação com release), Audit, Notification.
/// </summary>
public sealed record CostAnomalyDetectedEvent(
    Guid AnomalyId,
    string ServiceName,
    decimal ExpectedCost,
    decimal ActualCost,
    DateTimeOffset DetectedAt) : IntegrationEventBase("OperationalIntelligence");
