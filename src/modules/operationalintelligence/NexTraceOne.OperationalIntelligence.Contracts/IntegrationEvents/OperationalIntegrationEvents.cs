using NexTraceOne.BuildingBlocks.Core.Events;

namespace NexTraceOne.OperationalIntelligence.Contracts.IntegrationEvents;

/// <summary>
/// Publicado quando um incidente operacional é criado.
/// Consumidores: módulo de notificações (alertar owner e equipa operacional).
/// </summary>
public sealed record IncidentCreatedIntegrationEvent(
    Guid IncidentId,
    string ServiceName,
    string IncidentSeverity,
    string Description,
    Guid? OwnerUserId) : IntegrationEventBase("OperationalIntelligence");

/// <summary>
/// Publicado quando um incidente é escalado (severidade aumentada ou prazo expirado).
/// Consumidores: módulo de notificações (alertar gestores e equipa operacional).
/// </summary>
public sealed record IncidentEscalatedIntegrationEvent(
    Guid IncidentId,
    string ServiceName,
    string PreviousSeverity,
    string NewSeverity,
    Guid? OwnerUserId) : IntegrationEventBase("OperationalIntelligence");

/// <summary>
/// Publicado quando uma anomalia de custo é detetada que excede o orçamento.
/// Consumidores: módulo de notificações (alertar owner do serviço e gestores financeiros).
/// </summary>
public sealed record BudgetExceededIntegrationEvent(
    Guid AnomalyId,
    string ServiceName,
    decimal ExpectedCost,
    decimal ActualCost,
    Guid? OwnerUserId) : IntegrationEventBase("OperationalIntelligence");

/// <summary>
/// Publicado quando uma falha crítica de integração ou ingestão é detetada.
/// Consumidores: módulo de notificações (alertar owner da integração).
/// </summary>
public sealed record IntegrationFailedIntegrationEvent(
    Guid IntegrationId,
    string IntegrationName,
    string ErrorMessage,
    Guid? OwnerUserId) : IntegrationEventBase("OperationalIntelligence");
