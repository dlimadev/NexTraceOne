using Microsoft.EntityFrameworkCore;

using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de ComplianceGaps usando EF Core.
/// </summary>
internal sealed class ComplianceGapRepository(GovernanceDbContext context) : IComplianceGapRepository
{
    public async Task<IReadOnlyList<ComplianceGap>> ListAsync(
        string? teamId,
        string? domainId,
        string? serviceId,
        CancellationToken ct)
    {
        var query = context.ComplianceGaps.AsQueryable();

        if (!string.IsNullOrWhiteSpace(teamId))
            query = query.Where(g => g.Team == teamId);

        if (!string.IsNullOrWhiteSpace(domainId))
            query = query.Where(g => g.Domain == domainId);

        if (!string.IsNullOrWhiteSpace(serviceId))
            query = query.Where(g => g.ServiceId == serviceId);

        return await query
            .OrderByDescending(g => g.DetectedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(ComplianceGap gap, CancellationToken ct)
        => await context.ComplianceGaps.AddAsync(gap, ct);
}
