using Microsoft.EntityFrameworkCore;

using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Repositories;

/// <summary>
/// Repositório de release notes geradas por IA.
/// </summary>
internal sealed class ReleaseNotesRepository(ChangeIntelligenceDbContext context) : IReleaseNotesRepository
{
    /// <inheritdoc />
    public async Task AddAsync(ReleaseNotes notes, CancellationToken cancellationToken)
        => await context.ReleaseNotes.AddAsync(notes, cancellationToken);

    /// <inheritdoc />
    public async Task<ReleaseNotes?> GetByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken)
        => await context.ReleaseNotes
            .FirstOrDefaultAsync(n => n.ReleaseId == releaseId, cancellationToken);

    /// <inheritdoc />
    public void Update(ReleaseNotes notes)
        => context.ReleaseNotes.Update(notes);
}
