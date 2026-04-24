using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Application.Features.GetIngestionObservability;

/// <summary>
/// Feature: GetIngestionObservability — snapshot de saúde do pipeline de ingestão.
/// Retorna contagens reais da Dead Letter Queue derivadas de bb_dead_letter_messages.
/// </summary>
public static class GetIngestionObservability
{
    public sealed record Query() : IQuery<Response>;

    public sealed class Handler(IIngestionObservabilityProvider provider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var snapshot = await provider.GetSnapshotAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                Dlq: new DlqStatsDto(
                    Total: snapshot.Dlq.Total,
                    Pending: snapshot.Dlq.Pending,
                    Reprocessing: snapshot.Dlq.Reprocessing,
                    Resolved: snapshot.Dlq.Resolved,
                    Discarded: snapshot.Dlq.Discarded),
                CheckedAt: snapshot.CheckedAt));
        }
    }

    public sealed record Response(
        DlqStatsDto Dlq,
        DateTimeOffset CheckedAt);

    public sealed record DlqStatsDto(
        int Total,
        int Pending,
        int Reprocessing,
        int Resolved,
        int Discarded);
}
