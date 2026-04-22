using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Integrations.Application.Abstractions;

namespace NexTraceOne.Integrations.Application.Features.GetEventConsumerStatus;

/// <summary>
/// Feature: GetEventConsumerStatus — retorna o estado atual dos consumidores de eventos e fila dead letter.
/// Disponibiliza informações operacionais sobre o EventConsumerWorker para exibição no painel de integrações.
/// Ownership: módulo Integrations.
/// </summary>
public static class GetEventConsumerStatus
{
    /// <summary>Query para obter o estado dos consumidores de eventos.</summary>
    public sealed record Query() : IQuery<Response>;

    /// <summary>Handler que agrega estado dos consumidores e contagem de dead letters.</summary>
    public sealed class Handler(
        IEventConsumerStatusReader statusReader,
        IEventConsumerDeadLetterRepository deadLetterRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var statusEntries = await statusReader.GetStatusAsync(cancellationToken);
            var deadLetters = await deadLetterRepository.ListUnresolvedAsync(null, cancellationToken);

            var deadLetterCount = deadLetters.Count;
            var isHealthy = statusEntries.All(e =>
                string.Equals(e.Status, "Running", StringComparison.OrdinalIgnoreCase))
                && deadLetterCount == 0;

            var consumers = statusEntries
                .Select(e => new ConsumerStatus(
                    SourceType: e.SourceType,
                    Topic: e.Topic,
                    Status: e.Status,
                    ThroughputLast5Min: e.ThroughputLast5Min,
                    LastEventAt: e.LastEventAt,
                    DeadLetterCount: e.DeadLetterCount))
                .ToList();

            return Result<Response>.Success(new Response(
                Consumers: consumers,
                TotalDeadLetterCount: deadLetterCount,
                IsHealthy: isHealthy,
                CheckedAt: DateTimeOffset.UtcNow));
        }
    }

    /// <summary>Resposta com estado dos consumidores de eventos e totais de dead letter.</summary>
    public sealed record Response(
        IReadOnlyList<ConsumerStatus> Consumers,
        int TotalDeadLetterCount,
        bool IsHealthy,
        DateTimeOffset CheckedAt);

    /// <summary>Estado individual de um consumidor de eventos.</summary>
    public sealed record ConsumerStatus(
        string SourceType,
        string Topic,
        string Status,
        long ThroughputLast5Min,
        DateTimeOffset? LastEventAt,
        int DeadLetterCount);
}
