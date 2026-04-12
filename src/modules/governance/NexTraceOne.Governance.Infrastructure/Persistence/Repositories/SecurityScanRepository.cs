using Microsoft.EntityFrameworkCore;
using NexTraceOne.Governance.Application.SecurityGate.Ports;
using NexTraceOne.Governance.Domain.SecurityGate.Entities;
using NexTraceOne.Governance.Domain.SecurityGate.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório EF Core para SecurityScanResult e SecurityFinding.
/// </summary>
internal sealed class SecurityScanRepository(GovernanceDbContext context) : ISecurityScanRepository
{
    public async Task<SecurityScanResult?> FindByIdAsync(Guid scanId, CancellationToken ct)
        => await context.SecurityScanResults
            .Include(s => s.Findings)
            .FirstOrDefaultAsync(s => s.Id == new Domain.SecurityGate.SecurityScanResultId(scanId), ct);

    public async Task<IReadOnlyList<SecurityFinding>> ListFindingsAsync(
        Guid? targetId,
        FindingSeverity minSeverity,
        SecurityCategory? category,
        FindingStatus? status,
        int pageSize,
        int pageNumber,
        CancellationToken ct)
    {
        var query = context.SecurityFindings.AsQueryable();

        if (targetId.HasValue)
        {
            var scanIds = await context.SecurityScanResults
                .Where(s => s.TargetId == targetId.Value)
                .Select(s => s.Id)
                .ToListAsync(ct);
            query = query.Where(f => scanIds.Contains(f.ScanResultId));
        }

        query = query.Where(f => f.Severity >= minSeverity);

        if (category.HasValue)
            query = query.Where(f => f.Category == category.Value);

        if (status.HasValue)
            query = query.Where(f => f.Status == status.Value);

        return await query
            .OrderByDescending(f => f.Severity)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<(int TotalScans, int PassedScans)> GetScanCountsAsync(CancellationToken ct)
    {
        var total = await context.SecurityScanResults.CountAsync(ct);
        var passed = await context.SecurityScanResults.CountAsync(s => s.PassedGate, ct);
        return (total, passed);
    }

    public async Task<IReadOnlyList<(string Category, int Count)>> GetTopCategoriesAsync(int topN, CancellationToken ct)
    {
        var results = await context.SecurityFindings
            .GroupBy(f => f.Category)
            .Select(g => new { Category = g.Key.ToString(), Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(topN)
            .ToListAsync(ct);

        return results.Select(r => (r.Category, r.Count)).ToList();
    }

    public async Task AddAsync(SecurityScanResult scanResult, CancellationToken ct)
        => await context.SecurityScanResults.AddAsync(scanResult, ct);

    public Task UpdateAsync(SecurityScanResult scanResult, CancellationToken ct)
    {
        context.SecurityScanResults.Update(scanResult);
        return Task.CompletedTask;
    }
}
