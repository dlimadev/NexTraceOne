using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.BuildingBlocks.EventBus.Outbox;

/// <summary>
/// Implementação do IEventBus que persiste eventos no Outbox.
/// Garante at-least-once delivery mesmo em caso de falha do processo.
/// O OutboxProcessorJob consome e entrega os eventos de forma assíncrona.
/// </summary>
public sealed class OutboxEventBus : IEventBus
{
    public Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default) where T : class
        => throw new NotSupportedException("Use the DbContext outbox pipeline for persistent event delivery.");
}
