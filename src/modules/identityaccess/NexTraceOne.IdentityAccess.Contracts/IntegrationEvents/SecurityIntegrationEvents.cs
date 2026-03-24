using NexTraceOne.BuildingBlocks.Core.Events;

namespace NexTraceOne.IdentityAccess.Contracts.IntegrationEvents;

// ── Phase 5: High-Value Domain Events ──

/// <summary>
/// Publicado quando acesso JIT é concedido a um utilizador.
/// Consumidores: módulo de notificações (informar utilizador e admins).
/// </summary>
public sealed record JitAccessGrantedIntegrationEvent(
    Guid UserId,
    string Resource,
    string GrantedBy,
    DateTimeOffset ExpiresAt,
    Guid? TenantId) : IntegrationEventBase("Identity");

/// <summary>
/// Publicado quando uma revisão de acesso fica pendente.
/// Consumidores: módulo de notificações (alertar admins e equipa de segurança).
/// </summary>
public sealed record AccessReviewPendingIntegrationEvent(
    Guid ReviewId,
    string ReviewScope,
    DateTimeOffset DueDate,
    Guid? AssigneeUserId,
    Guid? TenantId) : IntegrationEventBase("Identity");
