using NexTraceOne.Integrations.Application.Abstractions;

namespace NexTraceOne.Integrations.Application.Services;

/// <summary>
/// Implementação nula do leitor de estado dos consumidores de eventos.
/// Retorna lista vazia enquanto nenhum consumer real estiver registado no worker.
/// </summary>
public sealed class NullEventConsumerStatusReader : IEventConsumerStatusReader
{
    /// <inheritdoc />
    public Task<IReadOnlyList<ConsumerStatusEntry>> GetStatusAsync(CancellationToken ct)
    {
        IReadOnlyList<ConsumerStatusEntry> empty = [];
        return Task.FromResult(empty);
    }
}
