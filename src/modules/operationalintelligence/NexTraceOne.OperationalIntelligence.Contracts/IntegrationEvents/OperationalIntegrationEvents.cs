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
    Guid? OwnerUserId,
    Guid? TenantId) : IntegrationEventBase("OperationalIntelligence");

/// <summary>
/// Publicado quando um incidente é escalado (severidade aumentada ou prazo expirado).
/// Consumidores: módulo de notificações (alertar gestores e equipa operacional).
/// </summary>
public sealed record IncidentEscalatedIntegrationEvent(
    Guid IncidentId,
    string ServiceName,
    string PreviousSeverity,
    string NewSeverity,
    Guid? OwnerUserId,
    Guid? TenantId) : IntegrationEventBase("OperationalIntelligence");

/// <summary>
/// Publicado quando uma anomalia de custo é detetada que excede o orçamento.
/// Consumidores: módulo de notificações (alertar owner do serviço e gestores financeiros).
/// </summary>
public sealed record BudgetExceededIntegrationEvent(
    Guid AnomalyId,
    string ServiceName,
    decimal ExpectedCost,
    decimal ActualCost,
    Guid? OwnerUserId,
    Guid? TenantId) : IntegrationEventBase("OperationalIntelligence");

/// <summary>
/// Publicado quando uma falha crítica de integração ou ingestão é detetada.
/// Consumidores: módulo de notificações (alertar owner da integração).
/// </summary>
public sealed record IntegrationFailedIntegrationEvent(
    Guid IntegrationId,
    string IntegrationName,
    string ErrorMessage,
    Guid? OwnerUserId,
    Guid? TenantId) : IntegrationEventBase("OperationalIntelligence");

// ── Phase 5: High-Value Domain Events ──

/// <summary>
/// Publicado quando um incidente é resolvido.
/// Consumidores: módulo de notificações (informar owner e equipa operacional).
/// </summary>
public sealed record IncidentResolvedIntegrationEvent(
    Guid IncidentId,
    string ServiceName,
    string ResolvedBy,
    Guid? OwnerUserId,
    Guid? TenantId) : IntegrationEventBase("OperationalIntelligence");

/// <summary>
/// Publicado quando uma anomalia operacional é detetada (runtime, performance, drift).
/// Consumidores: módulo de notificações (alertar owner do serviço).
/// </summary>
public sealed record AnomalyDetectedIntegrationEvent(
    Guid AnomalyId,
    string ServiceName,
    string AnomalyType,
    string Description,
    Guid? OwnerUserId,
    Guid? TenantId) : IntegrationEventBase("OperationalIntelligence");

/// <summary>
/// Publicado quando degradação de health significativa é detetada num serviço.
/// Consumidores: módulo de notificações (alertar owner e equipa operacional).
/// </summary>
public sealed record HealthDegradationIntegrationEvent(
    Guid ServiceId,
    string ServiceName,
    string PreviousStatus,
    string CurrentStatus,
    Guid? OwnerUserId,
    Guid? TenantId) : IntegrationEventBase("OperationalIntelligence");

/// <summary>
/// Publicado quando autenticação de conector falha.
/// Consumidores: módulo de notificações (alertar owner da integração).
/// COMPATIBILIDADE TRANSITÓRIA (P2.5): Este evento pertence semanticamente ao módulo Integrations.
/// Definição canónica migrada para NexTraceOne.Integrations.Contracts.IntegrationEvents.
/// Esta cópia em OperationalIntelligence.Contracts é mantida apenas para evitar quebra de contratos
/// de publicadores OI que ainda não foram migrados para Integrations.
/// Remover em fase futura quando todos os publicadores usarem Integrations.Contracts.
/// </summary>
public sealed record ConnectorAuthFailedIntegrationEvent(
    Guid ConnectorId,
    string ConnectorName,
    string ErrorMessage,
    Guid? OwnerUserId,
    Guid? TenantId) : IntegrationEventBase("OperationalIntelligence");

/// <summary>
/// Publicado quando sincronização de integração falha.
/// Consumidores: módulo de notificações (alertar owner da integração).
/// COMPATIBILIDADE TRANSITÓRIA (P2.5): Este evento pertence semanticamente ao módulo Integrations.
/// Definição canónica migrada para NexTraceOne.Integrations.Contracts.IntegrationEvents.
/// Esta cópia em OperationalIntelligence.Contracts é mantida apenas para evitar quebra de contratos
/// de publicadores OI que ainda não foram migrados para Integrations.
/// Remover em fase futura quando todos os publicadores usarem Integrations.Contracts.
/// </summary>
public sealed record SyncFailedIntegrationEvent(
    Guid IntegrationId,
    string IntegrationName,
    string ErrorMessage,
    Guid? OwnerUserId,
    Guid? TenantId) : IntegrationEventBase("OperationalIntelligence");
