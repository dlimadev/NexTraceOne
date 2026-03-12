using Microsoft.EntityFrameworkCore;
using NexTraceOne.Audit.Application.Abstractions;
using NexTraceOne.Audit.Domain.Entities;

namespace NexTraceOne.Audit.Infrastructure.Persistence.Repositories;

internal sealed class AuditEventRepository(AuditDbContext context) : IAuditEventRepository
{
    public async Task<AuditEvent?> GetByIdAsync(AuditEventId id, CancellationToken cancellationToken)
        => await context.AuditEvents
            .Include(e => e.ChainLink)
            .SingleOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AuditEvent>> SearchAsync(
        string? sourceModule, string? actionType, DateTimeOffset? from, DateTimeOffset? to,
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.AuditEvents.Include(e => e.ChainLink).AsQueryable();

        if (!string.IsNullOrWhiteSpace(sourceModule))
            query = query.Where(e => e.SourceModule == sourceModule);

        if (!string.IsNullOrWhiteSpace(actionType))
            query = query.Where(e => e.ActionType == actionType);

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
            .Where(e => e.ResourceType == resourceType && e.ResourceId == resourceId)
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync(cancellationToken);

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
