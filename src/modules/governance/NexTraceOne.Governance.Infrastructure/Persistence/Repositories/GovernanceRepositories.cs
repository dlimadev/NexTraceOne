using Microsoft.EntityFrameworkCore;

using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.Integrations.Domain.Entities;

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
// NexTraceOne.Integrations.Infrastructure.Persistence.Repositories in P2.1.

/// <summary>
/// Implementação do repositório de IngestionSources usando EF Core.
/// </summary>
internal sealed class IngestionSourceRepository(GovernanceDbContext context) : IIngestionSourceRepository
{
    public async Task<IReadOnlyList<IngestionSource>> ListAsync(
        IntegrationConnectorId? connectorId,
        SourceStatus? status,
        FreshnessStatus? freshnessStatus,
        CancellationToken ct)
    {
        var query = context.IngestionSources.AsQueryable();

        if (connectorId is not null)
            query = query.Where(s => s.ConnectorId == connectorId);

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        if (freshnessStatus.HasValue)
            query = query.Where(s => s.FreshnessStatus == freshnessStatus.Value);

        return await query.OrderBy(s => s.Name).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<IngestionSource>> ListByConnectorIdAsync(
        IntegrationConnectorId connectorId,
        CancellationToken ct)
        => await context.IngestionSources
            .Where(s => s.ConnectorId == connectorId)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public async Task<IngestionSource?> GetByIdAsync(IngestionSourceId id, CancellationToken ct)
        => await context.IngestionSources.SingleOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IngestionSource?> GetByConnectorAndNameAsync(
        IntegrationConnectorId connectorId,
        string name,
        CancellationToken ct)
        => await context.IngestionSources
            .SingleOrDefaultAsync(s => s.ConnectorId == connectorId && s.Name == name, ct);

    public async Task AddAsync(IngestionSource source, CancellationToken ct)
        => await context.IngestionSources.AddAsync(source, ct);

    public Task UpdateAsync(IngestionSource source, CancellationToken ct)
    {
        context.IngestionSources.Update(source);
        return Task.CompletedTask;
    }

    public async Task<int> CountByFreshnessStatusAsync(FreshnessStatus status, CancellationToken ct)
        => await context.IngestionSources.CountAsync(s => s.FreshnessStatus == status, ct);
}

/// <summary>
/// Implementação do repositório de IngestionExecutions usando EF Core.
/// </summary>
internal sealed class IngestionExecutionRepository(GovernanceDbContext context) : IIngestionExecutionRepository
{
    public async Task<IReadOnlyList<IngestionExecution>> ListAsync(
        IntegrationConnectorId? connectorId,
        IngestionSourceId? sourceId,
        ExecutionResult? result,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = context.IngestionExecutions.AsQueryable();

        if (connectorId is not null)
            query = query.Where(e => e.ConnectorId == connectorId);

        if (sourceId is not null)
            query = query.Where(e => e.SourceId == sourceId);

        if (result.HasValue)
            query = query.Where(e => e.Result == result.Value);

        if (from.HasValue)
            query = query.Where(e => e.StartedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.StartedAt <= to.Value);

        return await query
            .OrderByDescending(e => e.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<int> CountAsync(
        IntegrationConnectorId? connectorId,
        IngestionSourceId? sourceId,
        ExecutionResult? result,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct)
    {
        var query = context.IngestionExecutions.AsQueryable();

        if (connectorId is not null)
            query = query.Where(e => e.ConnectorId == connectorId);

        if (sourceId is not null)
            query = query.Where(e => e.SourceId == sourceId);

        if (result.HasValue)
            query = query.Where(e => e.Result == result.Value);

        if (from.HasValue)
            query = query.Where(e => e.StartedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.StartedAt <= to.Value);

        return await query.CountAsync(ct);
    }

    public async Task<IReadOnlyList<IngestionExecution>> ListByConnectorIdAsync(
        IntegrationConnectorId connectorId,
        int limit,
        CancellationToken ct)
        => await context.IngestionExecutions
            .Where(e => e.ConnectorId == connectorId)
            .OrderByDescending(e => e.StartedAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<IngestionExecution?> GetByIdAsync(IngestionExecutionId id, CancellationToken ct)
        => await context.IngestionExecutions.SingleOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IngestionExecution?> GetLastByConnectorIdAsync(
        IntegrationConnectorId connectorId,
        CancellationToken ct)
        => await context.IngestionExecutions
            .Where(e => e.ConnectorId == connectorId)
            .OrderByDescending(e => e.StartedAt)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(IngestionExecution execution, CancellationToken ct)
        => await context.IngestionExecutions.AddAsync(execution, ct);

    public Task UpdateAsync(IngestionExecution execution, CancellationToken ct)
    {
        context.IngestionExecutions.Update(execution);
        return Task.CompletedTask;
    }

    public async Task<int> CountByResultInPeriodAsync(
        ExecutionResult result,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
        => await context.IngestionExecutions
            .CountAsync(e => e.Result == result && e.StartedAt >= from && e.StartedAt <= to, ct);
}

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

