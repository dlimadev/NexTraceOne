using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.IdentityAccess.Contracts.IntegrationEvents;
using NexTraceOne.IdentityAccess.Domain.Events;

namespace NexTraceOne.IdentityAccess.Infrastructure.EventHandlers;

/// <summary>
/// Handler para o domain event UserCreatedDomainEvent.
/// Responsabilidades:
/// 1. Publicar UserCreatedIntegrationEvent para outros módulos reagirem à criação de utilizador.
/// 2. Registar em log a criação para rastreabilidade operacional.
///
/// Fluxo de dispatch: User.Create() → RaiseDomainEvent → Outbox →
///   OutboxProcessorJob → IEventBus.PublishAsync{UserCreatedDomainEvent} → este handler.
/// </summary>
internal sealed class UserCreatedDomainEventHandler(
    IEventBus eventBus,
    ICurrentTenant currentTenant,
    ILogger<UserCreatedDomainEventHandler> logger)
    : IIntegrationEventHandler<UserCreatedDomainEvent>
{
    public async Task HandleAsync(UserCreatedDomainEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "User {UserId} created with email {Email}. Publishing integration event.",
            @event.UserId.Value, @event.Email);

        var integrationEvent = new UserCreatedIntegrationEvent(
            UserId: @event.UserId.Value,
            Email: @event.Email,
            TenantId: currentTenant.Id == Guid.Empty ? null : currentTenant.Id);

        await eventBus.PublishAsync(integrationEvent, ct);
    }
}
