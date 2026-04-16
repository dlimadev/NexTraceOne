using Microsoft.EntityFrameworkCore;

using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de EvidencePackages usando EF Core.
/// </summary>
internal sealed class EvidencePackageRepository(GovernanceDbContext context) : IEvidencePackageRepository
{
    public async Task<IReadOnlyList<EvidencePackage>> ListAsync(
        string? scope,
        EvidencePackageStatus? status,
        CancellationToken ct)
    {
        var query = context.EvidencePackages
            .Include(p => p.Items)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(scope))
            query = query.Where(p => p.Scope == scope);

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<EvidencePackage?> GetByIdAsync(EvidencePackageId id, CancellationToken ct)
        => await context.EvidencePackages
            .Include(p => p.Items)
            .SingleOrDefaultAsync(p => p.Id == id, ct);

    public async Task AddAsync(EvidencePackage package, CancellationToken ct)
        => await context.EvidencePackages.AddAsync(package, ct);

    public Task UpdateAsync(EvidencePackage package, CancellationToken ct)
    {
        context.EvidencePackages.Update(package);
        return Task.CompletedTask;
    }
}
