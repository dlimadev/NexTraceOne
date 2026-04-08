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

/// <summary>
/// Implementação do repositório de GovernanceDomains usando EF Core.
/// </summary>
internal sealed class GovernanceDomainRepository(GovernanceDbContext context) : IGovernanceDomainRepository
{
    public async Task<IReadOnlyList<GovernanceDomain>> ListAsync(DomainCriticality? criticality, CancellationToken ct)
    {
        var query = context.Domains.AsQueryable();

        if (criticality.HasValue)
            query = query.Where(d => d.Criticality == criticality.Value);

        return await query.OrderBy(d => d.DisplayName).ToListAsync(ct);
    }

    public async Task<GovernanceDomain?> GetByIdAsync(GovernanceDomainId id, CancellationToken ct)
        => await context.Domains.SingleOrDefaultAsync(d => d.Id == id, ct);

    public async Task<GovernanceDomain?> GetByNameAsync(string name, CancellationToken ct)
        => await context.Domains.SingleOrDefaultAsync(d => d.Name == name, ct);

    public async Task AddAsync(GovernanceDomain domain, CancellationToken ct)
        => await context.Domains.AddAsync(domain, ct);

    public Task UpdateAsync(GovernanceDomain domain, CancellationToken ct)
    {
        context.Domains.Update(domain);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Implementação do repositório de GovernancePacks usando EF Core.
/// </summary>
internal sealed class GovernancePackRepository(GovernanceDbContext context) : IGovernancePackRepository
{
    public async Task<IReadOnlyList<GovernancePack>> ListAsync(
        GovernanceRuleCategory? category,
        GovernancePackStatus? status,
        CancellationToken ct)
    {
        var query = context.Packs.AsQueryable();

        if (category.HasValue)
            query = query.Where(p => p.Category == category.Value);

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        return await query.OrderBy(p => p.DisplayName).ToListAsync(ct);
    }

    public async Task<GovernancePack?> GetByIdAsync(GovernancePackId id, CancellationToken ct)
        => await context.Packs.SingleOrDefaultAsync(p => p.Id == id, ct);

    public async Task<GovernancePack?> GetByNameAsync(string name, CancellationToken ct)
        => await context.Packs.SingleOrDefaultAsync(p => p.Name == name, ct);

    public async Task AddAsync(GovernancePack pack, CancellationToken ct)
        => await context.Packs.AddAsync(pack, ct);

    public Task UpdateAsync(GovernancePack pack, CancellationToken ct)
    {
        context.Packs.Update(pack);
        return Task.CompletedTask;
    }
}

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

/// <summary>
/// Implementação do repositório de GovernanceWaivers usando EF Core.
/// </summary>
internal sealed class GovernanceWaiverRepository(GovernanceDbContext context) : IGovernanceWaiverRepository
{
    public async Task<IReadOnlyList<GovernanceWaiver>> ListAsync(
        GovernancePackId? packId,
        WaiverStatus? status,
        CancellationToken ct)
    {
        var query = context.Waivers.AsQueryable();

        if (packId is not null)
            query = query.Where(w => w.PackId == packId);

        if (status.HasValue)
            query = query.Where(w => w.Status == status.Value);

        return await query.OrderByDescending(w => w.RequestedAt).ToListAsync(ct);
    }

    public async Task<GovernanceWaiver?> GetByIdAsync(GovernanceWaiverId id, CancellationToken ct)
        => await context.Waivers.SingleOrDefaultAsync(w => w.Id == id, ct);

    public async Task AddAsync(GovernanceWaiver waiver, CancellationToken ct)
        => await context.Waivers.AddAsync(waiver, ct);

    public Task UpdateAsync(GovernanceWaiver waiver, CancellationToken ct)
    {
        context.Waivers.Update(waiver);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Implementação do repositório de DelegatedAdministrations usando EF Core.
/// </summary>
internal sealed class DelegatedAdministrationRepository(GovernanceDbContext context) : IDelegatedAdministrationRepository
{
    public async Task<IReadOnlyList<DelegatedAdministration>> ListAsync(
        DelegationScope? scope,
        bool? isActive,
        CancellationToken ct)
    {
        var query = context.DelegatedAdministrations.AsQueryable();

        if (scope.HasValue)
            query = query.Where(d => d.Scope == scope.Value);

        if (isActive.HasValue)
            query = query.Where(d => d.IsActive == isActive.Value);

        return await query.OrderByDescending(d => d.GrantedAt).ToListAsync(ct);
    }

    public async Task<DelegatedAdministration?> GetByIdAsync(DelegatedAdministrationId id, CancellationToken ct)
        => await context.DelegatedAdministrations.SingleOrDefaultAsync(d => d.Id == id, ct);

    public async Task<IReadOnlyList<DelegatedAdministration>> ListByGranteeAsync(
        string granteeUserId,
        CancellationToken ct)
        => await context.DelegatedAdministrations
            .Where(d => d.GranteeUserId == granteeUserId && d.IsActive)
            .OrderByDescending(d => d.GrantedAt)
            .ToListAsync(ct);

    public async Task AddAsync(DelegatedAdministration delegation, CancellationToken ct)
        => await context.DelegatedAdministrations.AddAsync(delegation, ct);

    public Task UpdateAsync(DelegatedAdministration delegation, CancellationToken ct)
    {
        context.DelegatedAdministrations.Update(delegation);
        return Task.CompletedTask;
    }
}

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

/// <summary>
/// Implementação do repositório de GovernanceRolloutRecords usando EF Core.
/// </summary>
internal sealed class GovernanceRolloutRecordRepository(GovernanceDbContext context) : IGovernanceRolloutRecordRepository
{
    public async Task<IReadOnlyList<GovernanceRolloutRecord>> ListAsync(
        GovernancePackId? packId,
        GovernanceScopeType? scopeType,
        string? scopeValue,
        RolloutStatus? status,
        CancellationToken ct)
    {
        var query = context.RolloutRecords.AsQueryable();

        if (packId is not null)
            query = query.Where(r => r.PackId == packId);

        if (scopeType.HasValue)
            query = query.Where(r => r.ScopeType == scopeType.Value);

        if (!string.IsNullOrWhiteSpace(scopeValue))
            query = query.Where(r => r.Scope == scopeValue);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        return await query
            .OrderByDescending(r => r.InitiatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<GovernanceRolloutRecord>> ListByPackIdAsync(
        GovernancePackId packId,
        CancellationToken ct)
        => await context.RolloutRecords
            .Where(r => r.PackId == packId)
            .OrderByDescending(r => r.InitiatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<GovernanceRolloutRecord>> ListByVersionIdAsync(
        GovernancePackVersionId versionId,
        CancellationToken ct)
        => await context.RolloutRecords
            .Where(r => r.VersionId == versionId)
            .OrderByDescending(r => r.InitiatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<GovernanceRolloutRecord>> ListByStatusAsync(
        RolloutStatus status,
        CancellationToken ct)
        => await context.RolloutRecords
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.InitiatedAt)
            .ToListAsync(ct);

    public async Task<GovernanceRolloutRecord?> GetByIdAsync(GovernanceRolloutRecordId id, CancellationToken ct)
        => await context.RolloutRecords.SingleOrDefaultAsync(r => r.Id == id, ct);

    public async Task AddAsync(GovernanceRolloutRecord record, CancellationToken ct)
        => await context.RolloutRecords.AddAsync(record, ct);

    public Task UpdateAsync(GovernanceRolloutRecord record, CancellationToken ct)
    {
        context.RolloutRecords.Update(record);
        return Task.CompletedTask;
    }
}

// NOTE: IntegrationConnectorRepository was extracted to
// NOTE P2.2: IngestionSourceRepository and IngestionExecutionRepository were extracted to
// NexTraceOne.Integrations.Infrastructure.Persistence.Repositories in P2.2.

/// <summary>
/// Implementação EF Core do repositório de analytics para Governance Trends.
/// Fornece consultas agregadas para indicadores executivos.
/// </summary>
internal sealed class GovernanceAnalyticsRepository(GovernanceDbContext context) : IGovernanceAnalyticsRepository
{
    public async Task<IReadOnlyList<MonthlyCount>> GetWaiverCountsByMonthAsync(int months, CancellationToken ct)
    {
        var startDate = DateTimeOffset.UtcNow.AddMonths(-months);

        var data = await context.Waivers
            .Where(w => w.RequestedAt >= startDate)
            .GroupBy(w => new { w.RequestedAt.Year, w.RequestedAt.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Count = g.Count()
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync(ct);

        return data.Select(x => new MonthlyCount(
            Period: $"{x.Year}-{x.Month:D2}",
            Count: x.Count)).ToList();
    }

    public async Task<IReadOnlyList<MonthlyCount>> GetPublishedPackCountsByMonthAsync(int months, CancellationToken ct)
    {
        var startDate = DateTimeOffset.UtcNow.AddMonths(-months);

        var data = await context.Packs
            .Where(p => p.Status == GovernancePackStatus.Published && p.UpdatedAt >= startDate)
            .GroupBy(p => new { p.UpdatedAt.Year, p.UpdatedAt.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Count = g.Count()
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync(ct);

        return data.Select(x => new MonthlyCount(
            Period: $"{x.Year}-{x.Month:D2}",
            Count: x.Count)).ToList();
    }

    public async Task<IReadOnlyList<MonthlyCount>> GetRolloutCountsByMonthAsync(int months, CancellationToken ct)
    {
        var startDate = DateTimeOffset.UtcNow.AddMonths(-months);

        var data = await context.RolloutRecords
            .Where(r => r.InitiatedAt >= startDate)
            .GroupBy(r => new { r.InitiatedAt.Year, r.InitiatedAt.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Count = g.Count()
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync(ct);

        return data.Select(x => new MonthlyCount(
            Period: $"{x.Year}-{x.Month:D2}",
            Count: x.Count)).ToList();
    }

    public async Task<int> GetPendingWaiverCountAsync(CancellationToken ct)
        => await context.Waivers.CountAsync(w => w.Status == WaiverStatus.Pending, ct);

    public async Task<int> GetPublishedPackCountAsync(CancellationToken ct)
        => await context.Packs.CountAsync(p => p.Status == GovernancePackStatus.Published, ct);
}

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

/// <summary>
/// Implementação do repositório de PolicyAsCodeDefinition usando EF Core.
/// </summary>
internal sealed class PolicyAsCodeRepository(GovernanceDbContext context) : IPolicyAsCodeRepository
{
    public async Task<IReadOnlyList<PolicyAsCodeDefinition>> ListAsync(
        PolicyDefinitionStatus? status,
        PolicyEnforcementMode? enforcementMode,
        CancellationToken ct)
    {
        var query = context.PolicyAsCodeDefinitions.AsQueryable();

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (enforcementMode.HasValue)
            query = query.Where(p => p.EnforcementMode == enforcementMode.Value);

        return await query.OrderBy(p => p.Name).ToListAsync(ct);
    }

    public async Task<PolicyAsCodeDefinition?> GetByIdAsync(PolicyAsCodeDefinitionId id, CancellationToken ct)
        => await context.PolicyAsCodeDefinitions.SingleOrDefaultAsync(p => p.Id == id, ct);

    public async Task<PolicyAsCodeDefinition?> GetByNameAsync(string name, CancellationToken ct)
        => await context.PolicyAsCodeDefinitions.SingleOrDefaultAsync(p => p.Name == name, ct);

    public async Task AddAsync(PolicyAsCodeDefinition definition, CancellationToken ct)
        => await context.PolicyAsCodeDefinitions.AddAsync(definition, ct);

    public Task UpdateAsync(PolicyAsCodeDefinition definition, CancellationToken ct)
    {
        context.PolicyAsCodeDefinitions.Update(definition);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Implementação do repositório de Custom Dashboards usando EF Core.
/// </summary>
internal sealed class CustomDashboardRepository(GovernanceDbContext context) : ICustomDashboardRepository
{
    public async Task<IReadOnlyList<CustomDashboard>> ListAsync(string? persona, CancellationToken ct)
    {
        var query = context.CustomDashboards.AsQueryable();

        if (!string.IsNullOrWhiteSpace(persona))
            query = query.Where(d => d.Persona == persona);

        return await query.OrderByDescending(d => d.UpdatedAt).AsNoTracking().ToListAsync(ct);
    }

    public async Task<CustomDashboard?> GetByIdAsync(CustomDashboardId id, CancellationToken ct)
        => await context.CustomDashboards.SingleOrDefaultAsync(d => d.Id == id, ct);

    public async Task<int> CountAsync(string? persona, CancellationToken ct)
    {
        var query = context.CustomDashboards.AsQueryable();

        if (!string.IsNullOrWhiteSpace(persona))
            query = query.Where(d => d.Persona == persona);

        return await query.CountAsync(ct);
    }

    public async Task AddAsync(CustomDashboard dashboard, CancellationToken ct)
        => await context.CustomDashboards.AddAsync(dashboard, ct);

    public Task UpdateAsync(CustomDashboard dashboard, CancellationToken ct)
    {
        context.CustomDashboards.Update(dashboard);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Implementação do repositório de Technical Debt Items usando EF Core.
/// </summary>
internal sealed class TechnicalDebtRepository(GovernanceDbContext context) : ITechnicalDebtRepository
{
    public async Task<IReadOnlyList<TechnicalDebtItem>> ListAsync(
        string? serviceName,
        string? debtType,
        int topN,
        CancellationToken ct)
    {
        var query = context.TechnicalDebtItems.AsQueryable();

        if (!string.IsNullOrWhiteSpace(serviceName))
            query = query.Where(d => d.ServiceName == serviceName);

        if (!string.IsNullOrWhiteSpace(debtType))
            query = query.Where(d => d.DebtType == debtType);

        return await query
            .OrderByDescending(d => d.DebtScore)
            .ThenByDescending(d => d.CreatedAt)
            .Take(topN)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<TechnicalDebtItem?> GetByIdAsync(TechnicalDebtItemId id, CancellationToken ct)
        => await context.TechnicalDebtItems.SingleOrDefaultAsync(d => d.Id == id, ct);

    public async Task AddAsync(TechnicalDebtItem item, CancellationToken ct)
        => await context.TechnicalDebtItems.AddAsync(item, ct);

    public Task UpdateAsync(TechnicalDebtItem item, CancellationToken ct)
    {
        context.TechnicalDebtItems.Update(item);
        return Task.CompletedTask;
    }
}
