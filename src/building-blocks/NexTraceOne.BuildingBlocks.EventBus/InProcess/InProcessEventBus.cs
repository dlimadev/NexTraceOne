using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.EventBus.Abstractions;

namespace NexTraceOne.BuildingBlocks.EventBus.InProcess;

/// <summary>
/// Implementação in-process do IEventBus usando handlers registrados no contêiner.
/// Usada no modo Modular Monolith — todos os módulos no mesmo processo.
/// Na evolução para microserviços, será substituída por RabbitMQ/Kafka.
/// </summary>
public sealed class InProcessEventBus(IServiceProvider serviceProvider) : IEventBus
{
    /// <summary>Publica um evento para todos os handlers registrados no processo atual.</summary>
    public async Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var handlers = serviceProvider.GetServices<IIntegrationEventHandler<T>>().ToArray();
        foreach (var handler in handlers)
        {
            await handler.HandleAsync(integrationEvent, ct);
        }
    }
}
