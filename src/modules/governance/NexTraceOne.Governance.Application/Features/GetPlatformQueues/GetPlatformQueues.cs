using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetPlatformQueues;

/// <summary>
/// Feature: GetPlatformQueues — estado das filas internas da plataforma.
/// Expõe contagens de mensagens pendentes, em processamento, falhadas e dead-letter
/// para cada fila, permitindo monitorização operacional proativa.
/// </summary>
public static class GetPlatformQueues
{
    /// <summary>Query sem parâmetros — retorna estado de todas as filas internas.</summary>
    public sealed record Query() : IQuery<Response>;

    /// <summary>Handler que agrega métricas de todas as filas internas da plataforma.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // TODO [P03.5]: Replace static queue snapshots with real queue telemetry provider
            // once platform queue metrics contract is available for Governance cross-module consumption.
            var now = DateTimeOffset.UtcNow;

            var queues = new List<QueueSummaryDto>
            {
                new(
                    QueueName: "outbox",
                    PendingCount: 12,
                    ProcessingCount: 3,
                    FailedCount: 0,
                    DeadLetterCount: 0,
                    AverageProcessingMs: 45.2,
                    LastActivityAt: now.AddSeconds(-15)),
                new(
                    QueueName: "ingestion",
                    PendingCount: 247,
                    ProcessingCount: 8,
                    FailedCount: 2,
                    DeadLetterCount: 1,
                    AverageProcessingMs: 320.7,
                    LastActivityAt: now.AddSeconds(-3)),
                new(
                    QueueName: "ai-requests",
                    PendingCount: 5,
                    ProcessingCount: 2,
                    FailedCount: 0,
                    DeadLetterCount: 0,
                    AverageProcessingMs: 1250.0,
                    LastActivityAt: now.AddMinutes(-1)),
                new(
                    QueueName: "analytics",
                    PendingCount: 89,
                    ProcessingCount: 4,
                    FailedCount: 1,
                    DeadLetterCount: 0,
                    AverageProcessingMs: 180.5,
                    LastActivityAt: now.AddSeconds(-8))
            };

            var response = new Response(
                Queues: queues,
                CheckedAt: now);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com estado de todas as filas internas.</summary>
    public sealed record Response(
        IReadOnlyList<QueueSummaryDto> Queues,
        DateTimeOffset CheckedAt);

    /// <summary>Resumo operacional de uma fila interna da plataforma.</summary>
    public sealed record QueueSummaryDto(
        string QueueName,
        long PendingCount,
        int ProcessingCount,
        long FailedCount,
        long DeadLetterCount,
        double AverageProcessingMs,
        DateTimeOffset LastActivityAt);
}
