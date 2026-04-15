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

// ── Phase 7: ServiceInterface & ContractBinding Domain Events ──

/// <summary>
/// Publicado quando uma nova interface de serviço é criada.
/// Consumidores: módulo de knowledge (indexar interface), módulo de AI (grounding), notificações.
/// </summary>
public sealed record ServiceInterfaceCreatedIntegrationEvent(
    Guid InterfaceId,
    Guid ServiceAssetId,
    string ServiceName,
    string InterfaceName,
    string InterfaceType,
    string ExposureScope,
    bool RequiresContract,
    string CreatedBy,
    Guid? TenantId) : IntegrationEventBase("Catalog");

/// <summary>
/// Publicado quando uma interface de serviço é marcada como depreciada.
/// Consumidores: notificações (avisar consumidores/owners), módulo de changes (risco de breaking change),
/// módulo de AI (ajustar análise de impacto).
/// </summary>
public sealed record ServiceInterfaceDeprecatedIntegrationEvent(
    Guid InterfaceId,
    Guid ServiceAssetId,
    string ServiceName,
    string InterfaceName,
    string InterfaceType,
    DateTimeOffset? DeprecationDate,
    DateTimeOffset? SunsetDate,
    string? DeprecationNotice,
    Guid? TenantId) : IntegrationEventBase("Catalog");

/// <summary>
/// Publicado quando uma versão de contrato é vinculada a uma interface de serviço.
/// Consumidores: módulo de changes (change confidence), notificações, módulo de AI (grounding de contratos).
/// </summary>
public sealed record ContractBoundToInterfaceIntegrationEvent(
    Guid BindingId,
    Guid ServiceInterfaceId,
    Guid ContractVersionId,
    string BindingEnvironment,
    bool IsDefaultVersion,
    string BoundBy,
    Guid? TenantId) : IntegrationEventBase("Catalog");

/// <summary>
/// Publicado quando um vínculo entre interface e versão de contrato é desactivado.
/// Consumidores: notificações (avisar consumidores), módulo de changes (reduzir confiança).
/// </summary>
public sealed record ContractBindingDeactivatedIntegrationEvent(
    Guid BindingId,
    Guid ServiceInterfaceId,
    Guid ContractVersionId,
    string BindingEnvironment,
    string DeactivatedBy,
    Guid? TenantId) : IntegrationEventBase("Catalog");
