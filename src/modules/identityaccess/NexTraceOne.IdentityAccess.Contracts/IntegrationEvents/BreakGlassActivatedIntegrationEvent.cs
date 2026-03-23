using NexTraceOne.BuildingBlocks.Core.Events;

namespace NexTraceOne.IdentityAccess.Contracts.IntegrationEvents;

/// <summary>
/// Publicado quando um acesso break-glass de emergência é ativado.
/// Consumidores: módulo de notificações (alertar admins do tenant imediatamente).
/// </summary>
public sealed record BreakGlassActivatedIntegrationEvent(
    Guid UserId,
    string ActivatedBy,
    string Resource,
    string Reason,
    Guid? TenantId) : IntegrationEventBase("Identity");
