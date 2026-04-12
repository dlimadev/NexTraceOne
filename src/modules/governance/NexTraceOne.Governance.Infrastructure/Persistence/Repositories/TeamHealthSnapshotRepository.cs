using Microsoft.EntityFrameworkCore;

using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de TeamHealthSnapshot usando EF Core.
/// </summary>
internal sealed class TeamHealthSnapshotRepository(GovernanceDbContext context)
    : ITeamHealthSnapshotRepository
{
    public async Task<TeamHealthSnapshot?> GetByIdAsync(
        TeamHealthSnapshotId id, CancellationToken ct)
        => await context.TeamHealthSnapshots.SingleOrDefaultAsync(s => s.Id == id, ct);

    public async Task<TeamHealthSnapshot?> GetByTeamIdAsync(
        Guid teamId, CancellationToken ct)
        => await context.TeamHealthSnapshots.SingleOrDefaultAsync(s => s.TeamId == teamId, ct);

    public async Task<IReadOnlyList<TeamHealthSnapshot>> ListAsync(
        int? minOverallScore, CancellationToken ct)
    {
        var query = context.TeamHealthSnapshots.AsQueryable();

        if (minOverallScore.HasValue)
            query = query.Where(s => s.OverallScore >= minOverallScore.Value);

        return await query
            .OrderBy(s => s.TeamName)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task AddAsync(TeamHealthSnapshot snapshot, CancellationToken ct)
        => await context.TeamHealthSnapshots.AddAsync(snapshot, ct);

    public Task UpdateAsync(TeamHealthSnapshot snapshot, CancellationToken ct)
    {
        context.TeamHealthSnapshots.Update(snapshot);
        return Task.CompletedTask;
    }
}
