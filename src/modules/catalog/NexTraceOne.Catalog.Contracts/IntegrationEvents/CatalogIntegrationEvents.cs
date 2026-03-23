using NexTraceOne.BuildingBlocks.Core.Events;

namespace NexTraceOne.Catalog.Contracts.IntegrationEvents;

// ── Phase 5: High-Value Domain Events ──

/// <summary>
/// Publicado quando um contrato de API/serviço é publicado.
/// Consumidores: módulo de notificações (informar owner e consumidores relevantes).
/// </summary>
public sealed record ContractPublishedIntegrationEvent(
    Guid ContractId,
    string ContractName,
    string ServiceName,
    string Version,
    Guid? PublisherUserId,
    Guid? TenantId) : IntegrationEventBase("Catalog");

/// <summary>
/// Publicado quando uma breaking change é detetada num contrato.
/// Consumidores: módulo de notificações (alertar owner e consumidores com destaque).
/// </summary>
public sealed record BreakingChangeDetectedIntegrationEvent(
    Guid ContractId,
    string ContractName,
    string ServiceName,
    string Description,
    Guid? OwnerUserId,
    Guid? TenantId) : IntegrationEventBase("Catalog");

/// <summary>
/// Publicado quando a validação de um contrato falha.
/// Consumidores: módulo de notificações (alertar editor/publisher do contrato).
/// </summary>
public sealed record ContractValidationFailedIntegrationEvent(
    Guid ContractId,
    string ContractName,
    string ServiceName,
    string ValidationError,
    Guid? OwnerUserId,
    Guid? TenantId) : IntegrationEventBase("Catalog");
