using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Application.Features.GetPlatformQueues;

/// <summary>
/// Feature: GetPlatformQueues — estado das filas internas da plataforma.
/// Expõe contagens reais de mensagens pendentes e falhadas por fila de outbox,
/// derivadas da tabela de outbox messages do módulo Governance.
/// </summary>
public static class GetPlatformQueues
{
    /// <summary>Query sem parâmetros — retorna estado de todas as filas acessíveis.</summary>
    public sealed record Query() : IQuery<Response>;

    /// <summary>Handler que retorna métricas reais de filas via IPlatformQueueMetricsProvider.</summary>
    public sealed class Handler(IPlatformQueueMetricsProvider metricsProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var snapshots = await metricsProvider.GetQueueSnapshotsAsync(cancellationToken);
            var now = DateTimeOffset.UtcNow;

            var queues = snapshots
                .Select(s => new QueueSummaryDto(
                    QueueName: s.QueueName,
                    PendingCount: s.PendingCount,
                    ProcessingCount: 0,
                    FailedCount: s.FailedCount,
                    DeadLetterCount: s.FailedCount,
                    AverageProcessingMs: 0.0,
                    LastActivityAt: s.LastActivityAt ?? now))
                .ToList();

            return Result<Response>.Success(new Response(Queues: queues, CheckedAt: now));
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
