using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;

namespace NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Outbox;

/// <summary>
/// Implementação do IEventBus invocada pelo OutboxProcessorJob para entregar eventos
/// que já foram persistidos no banco. Delega para handlers in-process registrados no contêiner.
/// FLUXO: DbContext persiste evento no Outbox → Job lê → OutboxEventBus entrega ao handler.
/// A persistência garantida é feita pelo NexTraceDbContextBase antes do commit.
/// </summary>
public sealed class OutboxEventBus(
    IServiceProvider serviceProvider,
    ILogger<OutboxEventBus> logger) : IEventBus
{
    /// <summary>Entrega o evento para todos os handlers in-process registrados.</summary>
    public async Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var handlers = serviceProvider.GetServices<IIntegrationEventHandler<T>>().ToArray();

        if (handlers.Length == 0)
        {
            logger.LogDebug(
                "No handlers registered for integration event {EventType}",
                typeof(T).Name);
            return;
        }

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(integrationEvent, ct);
        }
    }
}
