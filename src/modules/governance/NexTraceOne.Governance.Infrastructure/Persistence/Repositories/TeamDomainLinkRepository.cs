using Microsoft.EntityFrameworkCore;

using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de TeamDomainLinks usando EF Core.
/// </summary>
internal sealed class TeamDomainLinkRepository(GovernanceDbContext context) : ITeamDomainLinkRepository
{
    public async Task<IReadOnlyList<TeamDomainLink>> ListByTeamIdAsync(TeamId teamId, CancellationToken ct)
        => await context.TeamDomainLinks
            .Where(l => l.TeamId == teamId)
            .OrderBy(l => l.LinkedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TeamDomainLink>> ListByDomainIdAsync(GovernanceDomainId domainId, CancellationToken ct)
        => await context.TeamDomainLinks
            .Where(l => l.DomainId == domainId)
            .OrderBy(l => l.LinkedAt)
            .ToListAsync(ct);

    public async Task<TeamDomainLink?> GetByIdAsync(TeamDomainLinkId id, CancellationToken ct)
        => await context.TeamDomainLinks.SingleOrDefaultAsync(l => l.Id == id, ct);

    public async Task<TeamDomainLink?> GetByTeamAndDomainAsync(
        TeamId teamId,
        GovernanceDomainId domainId,
        CancellationToken ct)
        => await context.TeamDomainLinks
            .SingleOrDefaultAsync(l => l.TeamId == teamId && l.DomainId == domainId, ct);

    public async Task AddAsync(TeamDomainLink link, CancellationToken ct)
        => await context.TeamDomainLinks.AddAsync(link, ct);

    public Task RemoveAsync(TeamDomainLink link, CancellationToken ct)
    {
        context.TeamDomainLinks.Remove(link);
        return Task.CompletedTask;
    }
}
