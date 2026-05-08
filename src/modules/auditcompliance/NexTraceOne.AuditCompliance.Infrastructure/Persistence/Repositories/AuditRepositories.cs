using Microsoft.EntityFrameworkCore;

using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Domain.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AuditCompliance.Infrastructure.Persistence.Repositories;

internal sealed class AuditEventRepository(AuditDbContext context, ICurrentTenant currentTenant) : IAuditEventRepository
{
    public async Task<AuditEvent?> GetByIdAsync(AuditEventId id, CancellationToken cancellationToken)
        => await context.AuditEvents
            .Include(e => e.ChainLink)
            .SingleOrDefaultAsync(e => e.Id == id && e.TenantId == currentTenant.Id, cancellationToken);

    public async Task<IReadOnlyList<AuditEvent>> SearchAsync(
        string? sourceModule,
        string? actionType,
        string? correlationId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = context.AuditEvents
            .Include(e => e.ChainLink)
            .Where(e => e.TenantId == currentTenant.Id);

        if (!string.IsNullOrWhiteSpace(sourceModule))
            query = query.Where(e => e.SourceModule == sourceModule);

        if (!string.IsNullOrWhiteSpace(actionType))
            query = query.Where(e => e.ActionType == actionType);

        if (!string.IsNullOrWhiteSpace(correlationId))
            query = query.Where(e => e.CorrelationId == correlationId);

        if (from.HasValue)
            query = query.Where(e => e.OccurredAt >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.OccurredAt <= to.Value);

        return await query
            .OrderByDescending(e => e.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEvent>> GetTrailByResourceAsync(string resourceType, string resourceId, CancellationToken cancellationToken)
        => await context.AuditEvents
            .Include(e => e.ChainLink)
            .Where(e => e.TenantId == currentTenant.Id && e.ResourceType == resourceType && e.ResourceId == resourceId)
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AuditEvent>> SearchWithResourceAsync(
        string? sourceModule,
        string? actionType,
        string? correlationId,
        string? resourceType, string? resourceId,
        DateTimeOffset? from, DateTimeOffset? to,
        int page, int pageSize,
        CancellationToken cancellationToken)
    {
        var query = context.AuditEvents
            .Include(e => e.ChainLink)
            .Where(e => e.TenantId == currentTenant.Id);

        if (!string.IsNullOrWhiteSpace(sourceModule))
            query = query.Where(e => e.SourceModule == sourceModule);

        if (!string.IsNullOrWhiteSpace(actionType))
            query = query.Where(e => e.ActionType == actionType);

        if (!string.IsNullOrWhiteSpace(correlationId))
            query = query.Where(e => e.CorrelationId == correlationId);

        if (!string.IsNullOrWhiteSpace(resourceType))
            query = query.Where(e => e.ResourceType == resourceType);

        if (!string.IsNullOrWhiteSpace(resourceId))
            query = query.Where(e => e.ResourceId == resourceId);

        if (from.HasValue)
            query = query.Where(e => e.OccurredAt >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.OccurredAt <= to.Value);

        return await query
            .OrderByDescending(e => e.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(
        string? sourceModule,
        string? actionType,
        string? correlationId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var query = context.AuditEvents.Where(e => e.TenantId == currentTenant.Id);

        if (!string.IsNullOrWhiteSpace(sourceModule))
            query = query.Where(e => e.SourceModule == sourceModule);

        if (!string.IsNullOrWhiteSpace(actionType))
            query = query.Where(e => e.ActionType == actionType);

        if (!string.IsNullOrWhiteSpace(correlationId))
            query = query.Where(e => e.CorrelationId == correlationId);

        if (from.HasValue)
            query = query.Where(e => e.OccurredAt >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.OccurredAt <= to.Value);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<int> CountWithResourceAsync(
        string? sourceModule,
        string? actionType,
        string? correlationId,
        string? resourceType,
        string? resourceId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var query = context.AuditEvents.Where(e => e.TenantId == currentTenant.Id);

        if (!string.IsNullOrWhiteSpace(sourceModule))
            query = query.Where(e => e.SourceModule == sourceModule);

        if (!string.IsNullOrWhiteSpace(actionType))
            query = query.Where(e => e.ActionType == actionType);

        if (!string.IsNullOrWhiteSpace(correlationId))
            query = query.Where(e => e.CorrelationId == correlationId);

        if (!string.IsNullOrWhiteSpace(resourceType))
            query = query.Where(e => e.ResourceType == resourceType);

        if (!string.IsNullOrWhiteSpace(resourceId))
            query = query.Where(e => e.ResourceId == resourceId);

        if (from.HasValue)
            query = query.Where(e => e.OccurredAt >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.OccurredAt <= to.Value);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<int> DeleteExpiredAsync(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        // ExecuteDeleteAsync (EF Core 7+) performs a single SQL DELETE without loading entities into memory.
        // Intentionally not filtered by tenant — this is a platform-level retention job
        // that runs without a tenant context and must clean up all tenants' expired events.
        return await context.AuditEvents
            .Where(e => e.OccurredAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public void Add(AuditEvent auditEvent) => context.AuditEvents.Add(auditEvent);
}

internal sealed class AuditChainRepository(AuditDbContext context) : IAuditChainRepository
{
    public async Task<AuditChainLink?> GetLatestLinkAsync(CancellationToken cancellationToken)
        => await context.AuditChainLinks
            .OrderByDescending(l => l.SequenceNumber)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<AuditChainLink>> GetAllLinksAsync(CancellationToken cancellationToken)
        => await context.AuditChainLinks
            .OrderBy(l => l.SequenceNumber)
            .ToListAsync(cancellationToken);

    public void Add(AuditChainLink chainLink) => context.AuditChainLinks.Add(chainLink);
}
