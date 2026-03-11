using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.BuildingBlocks.EventBus.InProcess;

/// <summary>
/// Implementação in-process do IEventBus usando MediatR.
/// Usada no modo Modular Monolith — todos os módulos no mesmo processo.
/// Na evolução para microserviços, será substituída por RabbitMQ/Kafka.
/// </summary>
public sealed class InProcessEventBus : IEventBus
{
    public Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default) where T : class
    {
        // TODO: Implementar publicação via MediatR Publish
        throw new NotImplementedException();
    }
}
