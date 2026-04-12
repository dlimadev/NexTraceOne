using Microsoft.EntityFrameworkCore;

using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de LicenseComplianceReport usando EF Core.
/// </summary>
internal sealed class LicenseComplianceReportRepository(GovernanceDbContext context)
    : ILicenseComplianceReportRepository
{
    public async Task<LicenseComplianceReport?> GetByIdAsync(
        LicenseComplianceReportId id, CancellationToken ct)
        => await context.LicenseComplianceReports.SingleOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<LicenseComplianceReport>> ListByScopeAsync(
        LicenseComplianceScope scope,
        string? scopeKey,
        CancellationToken ct)
    {
        var query = context.LicenseComplianceReports
            .Where(r => r.Scope == scope);

        if (scopeKey is not null)
            query = query.Where(r => r.ScopeKey == scopeKey);

        return await query
            .OrderByDescending(r => r.ScannedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<LicenseComplianceReport?> GetLatestByScopeKeyAsync(
        string scopeKey, CancellationToken ct)
        => await context.LicenseComplianceReports
            .Where(r => r.ScopeKey == scopeKey)
            .OrderByDescending(r => r.ScannedAt)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(LicenseComplianceReport report, CancellationToken ct)
        => await context.LicenseComplianceReports.AddAsync(report, ct);
}
