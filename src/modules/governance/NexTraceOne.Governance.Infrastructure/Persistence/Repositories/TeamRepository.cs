using Microsoft.EntityFrameworkCore;

using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de Teams usando EF Core.
/// </summary>
internal sealed class TeamRepository(GovernanceDbContext context) : ITeamRepository
{
    public async Task<IReadOnlyList<Team>> ListAsync(TeamStatus? status, CancellationToken ct)
    {
        var query = context.Teams.AsQueryable();

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        return await query.OrderBy(t => t.DisplayName).ToListAsync(ct);
    }

    public async Task<Team?> GetByIdAsync(TeamId id, CancellationToken ct)
        => await context.Teams.SingleOrDefaultAsync(t => t.Id == id, ct);

    public async Task<Team?> GetByNameAsync(string name, CancellationToken ct)
        => await context.Teams.SingleOrDefaultAsync(t => t.Name == name, ct);

    public async Task AddAsync(Team team, CancellationToken ct)
        => await context.Teams.AddAsync(team, ct);

    public Task UpdateAsync(Team team, CancellationToken ct)
    {
        context.Teams.Update(team);
        return Task.CompletedTask;
    }
}
