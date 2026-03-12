using Microsoft.EntityFrameworkCore;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Infrastructure.Persistence;

namespace NexTraceOne.Identity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de eventos de segurança.
/// </summary>
internal sealed class SecurityEventRepository(IdentityDbContext dbContext) : ISecurityEventRepository
{
    public async Task<IReadOnlyList<SecurityEvent>> ListByTenantAsync(
        TenantId tenantId,
        string? eventType,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.SecurityEvents
            .Where(x => x.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            query = query.Where(x => x.EventType == eventType);
        }

        return await query
            .OrderByDescending(x => x.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountUnreviewedByTenantAsync(TenantId tenantId, CancellationToken cancellationToken)
        => await dbContext.SecurityEvents
            .CountAsync(x => x.TenantId == tenantId && !x.IsReviewed, cancellationToken);

    public void Add(SecurityEvent securityEvent)
        => dbContext.SecurityEvents.Add(securityEvent);
}
