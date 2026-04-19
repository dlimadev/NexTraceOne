using Microsoft.EntityFrameworkCore;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório de jobs de recovery.</summary>
internal sealed class RecoveryJobRepository(GovernanceDbContext context) : IRecoveryJobRepository
{
    public async Task<IReadOnlyList<RecoveryJob>> ListAsync(int limit, CancellationToken ct)
        => await context.RecoveryJobs
            .AsNoTracking()
            .OrderByDescending(j => j.InitiatedAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<RecoveryJob?> GetByIdAsync(RecoveryJobId id, CancellationToken ct)
        => await context.RecoveryJobs
            .FirstOrDefaultAsync(j => j.Id == id, ct);

    public async Task AddAsync(RecoveryJob job, CancellationToken ct)
        => await context.RecoveryJobs.AddAsync(job, ct);

    public void Update(RecoveryJob job)
        => context.RecoveryJobs.Update(job);
}
