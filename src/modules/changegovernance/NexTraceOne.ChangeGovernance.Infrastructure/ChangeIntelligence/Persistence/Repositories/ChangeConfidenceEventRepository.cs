using Microsoft.EntityFrameworkCore;

using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Repositories;

/// <summary>
/// Repositório de eventos de confiança de mudanças (append-only).
/// </summary>
internal sealed class ChangeConfidenceEventRepository(ChangeIntelligenceDbContext context) : IChangeConfidenceEventRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ChangeConfidenceEvent>> ListByReleaseAsync(ReleaseId releaseId, CancellationToken cancellationToken)
        => await context.ChangeConfidenceEvents
            .Where(e => e.ReleaseId == releaseId)
            .OrderBy(e => e.OccurredAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<ChangeConfidenceEvent?> GetLatestByReleaseAsync(ReleaseId releaseId, CancellationToken cancellationToken)
        => await context.ChangeConfidenceEvents
            .Where(e => e.ReleaseId == releaseId)
            .OrderByDescending(e => e.OccurredAt)
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(ChangeConfidenceEvent confidenceEvent, CancellationToken cancellationToken)
        => await context.ChangeConfidenceEvents.AddAsync(confidenceEvent, cancellationToken);
}
