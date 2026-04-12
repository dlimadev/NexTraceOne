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

// ── Phase 6: Contract Verification & Changelog Events ──

/// <summary>
/// Publicado quando a verificação de um contrato é bem-sucedida (sem breaking changes).
/// Consumidores: módulo de changes (aumentar change confidence), notificações.
/// </summary>
public sealed record ContractVerificationPassedIntegrationEvent(
    Guid VerificationId,
    string ApiAssetId,
    string ServiceName,
    string SourceSystem,
    string? CommitSha,
    Guid? TenantId) : IntegrationEventBase("Catalog");

/// <summary>
/// Publicado quando a verificação de um contrato deteta breaking changes.
/// Consumidores: módulo de changes (reduzir change confidence), notificações, risk center.
/// </summary>
public sealed record ContractVerificationFailedIntegrationEvent(
    Guid VerificationId,
    string ApiAssetId,
    string ServiceName,
    string SourceSystem,
    int BreakingChangesCount,
    string? CommitSha,
    Guid? TenantId) : IntegrationEventBase("Catalog");

/// <summary>
/// Publicado quando um changelog de contrato é gerado automaticamente.
/// Consumidores: módulo de knowledge (indexar changelog), notificações.
/// </summary>
public sealed record ContractChangelogGeneratedIntegrationEvent(
    Guid ChangelogId,
    string ApiAssetId,
    string ServiceName,
    string ToVersion,
    int EntryCount,
    Guid? TenantId) : IntegrationEventBase("Catalog");
