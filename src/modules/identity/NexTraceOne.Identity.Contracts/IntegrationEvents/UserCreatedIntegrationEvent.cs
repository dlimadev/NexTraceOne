using NexTraceOne.BuildingBlocks.Domain;

namespace NexTraceOne.Identity.Contracts.IntegrationEvents;

/// <summary>
/// Evento de integração publicado quando um usuário é criado.
/// Consumidores: módulos que precisam reagir à criação de usuários (ex: DeveloperPortal).
/// </summary>
public sealed record UserCreatedIntegrationEvent(Guid UserId, string Email, Guid TenantId)
    : IntegrationEventBase("Identity");
