using Microsoft.EntityFrameworkCore;

using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de GovernancePackVersions usando EF Core.
/// </summary>
internal sealed class GovernancePackVersionRepository(GovernanceDbContext context) : IGovernancePackVersionRepository
{
    public async Task<IReadOnlyList<GovernancePackVersion>> ListByPackIdAsync(GovernancePackId packId, CancellationToken ct)
        => await context.PackVersions
            .Where(v => v.PackId == packId)
            .OrderByDescending(v => v.PublishedAt)
            .ToListAsync(ct);

    public async Task<GovernancePackVersion?> GetByIdAsync(GovernancePackVersionId id, CancellationToken ct)
        => await context.PackVersions.SingleOrDefaultAsync(v => v.Id == id, ct);

    public async Task<GovernancePackVersion?> GetLatestByPackIdAsync(GovernancePackId packId, CancellationToken ct)
        => await context.PackVersions
            .Where(v => v.PackId == packId)
            .OrderByDescending(v => v.PublishedAt)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(GovernancePackVersion version, CancellationToken ct)
        => await context.PackVersions.AddAsync(version, ct);
}
