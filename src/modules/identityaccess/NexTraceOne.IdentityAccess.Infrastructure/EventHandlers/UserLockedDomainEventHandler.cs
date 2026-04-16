using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Events;

namespace NexTraceOne.IdentityAccess.Infrastructure.EventHandlers;

/// <summary>
/// Handler para o domain event UserLockedDomainEvent.
/// Responsabilidades:
/// 1. Registar o evento de bloqueio em log no nível WARNING para alertas de segurança.
/// 2. Permitir extensão futura para publicação de integration event de lock-out.
///
/// Fluxo de dispatch: User.RecordFailedLogin() → RaiseDomainEvent(UserLockedDomainEvent) →
///   Outbox → OutboxProcessorJob → IEventBus.PublishAsync{UserLockedDomainEvent} → este handler.
/// </summary>
internal sealed class UserLockedDomainEventHandler(
    ILogger<UserLockedDomainEventHandler> logger)
    : IIntegrationEventHandler<UserLockedDomainEvent>
{
    public Task HandleAsync(UserLockedDomainEvent @event, CancellationToken ct = default)
    {
        logger.LogWarning(
            "Account locked for user {UserId}. Lockout ends at {LockoutEnd}. " +
            "This may indicate a brute-force attack — review security events.",
            @event.UserId.Value, @event.LockoutEnd);

        return Task.CompletedTask;
    }
}
