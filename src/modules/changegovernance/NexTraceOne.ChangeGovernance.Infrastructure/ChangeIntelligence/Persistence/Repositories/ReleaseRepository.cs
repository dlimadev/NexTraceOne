using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Repositories;

/// <summary>
/// Repositório de releases, implementando consultas específicas de negócio.
/// </summary>
internal sealed class ReleaseRepository(ChangeIntelligenceDbContext context)
    : RepositoryBase<Release, ReleaseId>(context), IReleaseRepository
{
    /// <summary>Busca uma Release pelo seu identificador.</summary>
    public override async Task<Release?> GetByIdAsync(ReleaseId id, CancellationToken ct = default)
        => await context.Releases
            .SingleOrDefaultAsync(r => r.Id == id, ct);

    /// <summary>Busca releases de um ativo de API por versão e ambiente.</summary>
    public async Task<Release?> GetByApiAssetAndVersionAsync(Guid apiAssetId, string version, string environment, CancellationToken cancellationToken = default)
        => await context.Releases
            .SingleOrDefaultAsync(r => r.ApiAssetId == apiAssetId
                && r.Version == version
                && r.Environment == environment, cancellationToken);

    /// <inheritdoc />
    public async Task<Release?> GetByServiceNameVersionEnvironmentAsync(string serviceName, string version, string environment, CancellationToken cancellationToken = default)
        => await context.Releases
            .SingleOrDefaultAsync(r => r.ServiceName == serviceName
                && r.Version == version
                && r.Environment == environment, cancellationToken);

    /// <summary>Lista releases de um ativo de API ordenadas por data de criação descendente.</summary>
    public async Task<IReadOnlyList<Release>> ListByApiAssetAsync(Guid apiAssetId, int page, int pageSize, CancellationToken cancellationToken = default)
        => await context.Releases
            .Where(r => r.ApiAssetId == apiAssetId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    /// <summary>Conta o total de releases de um ativo de API.</summary>
    public async Task<int> CountByApiAssetAsync(Guid apiAssetId, CancellationToken cancellationToken = default)
        => await context.Releases
            .CountAsync(r => r.ApiAssetId == apiAssetId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Release>> ListFilteredAsync(
        Guid tenantId,
        string? serviceName, string? teamName, string? environment,
        ChangeType? changeType, ConfidenceStatus? confidenceStatus,
        DeploymentStatus? deploymentStatus, string? searchTerm,
        DateTimeOffset? from, DateTimeOffset? to,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = ApplyFilters(
            tenantId,
            serviceName, teamName, environment, changeType,
            confidenceStatus, deploymentStatus, searchTerm, from, to);

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountFilteredAsync(
        Guid tenantId,
        string? serviceName, string? teamName, string? environment,
        ChangeType? changeType, ConfidenceStatus? confidenceStatus,
        DeploymentStatus? deploymentStatus, string? searchTerm,
        DateTimeOffset? from, DateTimeOffset? to,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilters(
            tenantId,
            serviceName, teamName, environment, changeType,
            confidenceStatus, deploymentStatus, searchTerm, from, to);

        return await query.CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Release>> ListByServiceNameAsync(
        string serviceName, int page, int pageSize,
        CancellationToken cancellationToken = default)
        => await context.Releases
            .Where(r => r.ServiceName == serviceName)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<int> CountByServiceNameAsync(
        string serviceName, CancellationToken cancellationToken = default)
        => await context.Releases
            .CountAsync(r => r.ServiceName == serviceName, cancellationToken);

    /// <inheritdoc />
    public async Task<(int total, int validated, int needsAttention, int suspectedRegressions, int correlatedWithIncidents)>
        GetSummaryCountsAsync(
            Guid tenantId,
            string? teamName, string? environment,
            DateTimeOffset? from, DateTimeOffset? to,
            CancellationToken cancellationToken = default)
    {
        var query = context.Releases
            .Where(r => r.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(teamName))
            query = query.Where(r => r.TeamName == teamName);
        if (!string.IsNullOrWhiteSpace(environment))
            query = query.Where(r => r.Environment == environment);
        if (from.HasValue)
            query = query.Where(r => r.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(r => r.CreatedAt <= to.Value);

        var total = await query.CountAsync(cancellationToken);
        var validated = await query.CountAsync(r => r.ConfidenceStatus == ConfidenceStatus.Validated, cancellationToken);
        var needsAttention = await query.CountAsync(r => r.ConfidenceStatus == ConfidenceStatus.NeedsAttention, cancellationToken);
        var suspectedRegressions = await query.CountAsync(r => r.ConfidenceStatus == ConfidenceStatus.SuspectedRegression, cancellationToken);
        var correlatedWithIncidents = await query.CountAsync(r => r.ConfidenceStatus == ConfidenceStatus.CorrelatedWithIncident, cancellationToken);

        return (total, validated, needsAttention, suspectedRegressions, correlatedWithIncidents);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Release>> ListInRangeAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string? environment,
        CancellationToken cancellationToken = default)
    {
        var query = context.Releases
            .Where(r => r.CreatedAt >= from && r.CreatedAt <= to);

        if (!string.IsNullOrWhiteSpace(environment))
            query = query.Where(r => r.Environment == environment);

        return await query
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Release>> ListSimilarReleasesAsync(
        ReleaseId excludeReleaseId,
        string serviceName,
        string environment,
        ChangeLevel changeLevel,
        DateTimeOffset from,
        DateTimeOffset to,
        int maxResults,
        CancellationToken cancellationToken = default)
        => await context.Releases
            .Where(r => r.Id != excludeReleaseId
                && r.ServiceName == serviceName
                && r.Environment == environment
                && r.ChangeLevel == changeLevel
                && r.CreatedAt >= from
                && r.CreatedAt <= to)
            .OrderByDescending(r => r.CreatedAt)
            .Take(maxResults)
            .ToListAsync(cancellationToken);

    private IQueryable<Release> ApplyFilters(
        Guid tenantId,
        string? serviceName, string? teamName, string? environment,
        ChangeType? changeType, ConfidenceStatus? confidenceStatus,
        DeploymentStatus? deploymentStatus, string? searchTerm,
        DateTimeOffset? from, DateTimeOffset? to)
    {
        var query = context.Releases
            .Where(r => r.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(serviceName))
            query = query.Where(r => r.ServiceName == serviceName);
        if (!string.IsNullOrWhiteSpace(teamName))
            query = query.Where(r => r.TeamName == teamName);
        if (!string.IsNullOrWhiteSpace(environment))
            query = query.Where(r => r.Environment == environment);
        if (changeType.HasValue)
            query = query.Where(r => r.ChangeType == changeType.Value);
        if (confidenceStatus.HasValue)
            query = query.Where(r => r.ConfidenceStatus == confidenceStatus.Value);
        if (deploymentStatus.HasValue)
            query = query.Where(r => r.Status == deploymentStatus.Value);
        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(r => r.ServiceName.Contains(searchTerm)
                || (r.Description != null && r.Description.Contains(searchTerm))
                || r.Version.Contains(searchTerm));
        if (from.HasValue)
            query = query.Where(r => r.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(r => r.CreatedAt <= to.Value);

        return query;
    }
}
