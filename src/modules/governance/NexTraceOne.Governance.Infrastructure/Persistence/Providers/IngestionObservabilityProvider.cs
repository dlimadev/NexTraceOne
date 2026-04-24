using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Outbox;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Providers;

/// <summary>
/// Implementação de IIngestionObservabilityProvider usando BuildingBlocksDbContext.
/// Deriva contagens reais da tabela bb_dead_letter_messages agrupadas por status.
/// </summary>
internal sealed class IngestionObservabilityProvider(BuildingBlocksDbContext dbContext)
    : IIngestionObservabilityProvider
{
    public async Task<IngestionObservabilitySnapshot> GetSnapshotAsync(CancellationToken ct)
    {
        var counts = await dbContext.DeadLetterMessages
            .AsNoTracking()
            .GroupBy(m => m.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        int Get(DlqMessageStatus status) =>
            counts.FirstOrDefault(c => c.Status == status)?.Count ?? 0;

        var pending = Get(DlqMessageStatus.Pending);
        var reprocessing = Get(DlqMessageStatus.Reprocessing);
        var resolved = Get(DlqMessageStatus.Resolved);
        var discarded = Get(DlqMessageStatus.Discarded);

        return new IngestionObservabilitySnapshot(
            Dlq: new IngestionDlqStats(
                Total: pending + reprocessing + resolved + discarded,
                Pending: pending,
                Reprocessing: reprocessing,
                Resolved: resolved,
                Discarded: discarded),
            CheckedAt: DateTimeOffset.UtcNow);
    }
}
