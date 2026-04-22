namespace NexTraceOne.Integrations.Application.Abstractions;

/// <summary>
/// Leitor de estado dos consumidores de eventos registados no worker.
/// Fornece métricas operacionais em tempo real para o dashboard de integração.
/// </summary>
public interface IEventConsumerStatusReader
{
    /// <summary>Retorna a lista de estado atual de todos os consumidores registados.</summary>
    Task<IReadOnlyList<ConsumerStatusEntry>> GetStatusAsync(CancellationToken ct);
}

/// <summary>
/// Entrada de estado de um consumidor de eventos individual.
/// </summary>
public sealed record ConsumerStatusEntry(
    string SourceType,
    string Topic,
    string Status,
    long ThroughputLast5Min,
    DateTimeOffset? LastEventAt,
    int DeadLetterCount);
