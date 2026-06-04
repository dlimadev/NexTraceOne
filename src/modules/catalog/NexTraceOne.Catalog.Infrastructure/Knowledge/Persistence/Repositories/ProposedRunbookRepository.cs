using Microsoft.EntityFrameworkCore;
using NexTraceOne.Catalog.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.Knowledge.Abstractions;
using NexTraceOne.Catalog.Domain.Knowledge.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Knowledge.Persistence.Repositories;

internal sealed class ProposedRunbookRepository(ServiceCatalogDbContext db) : IProposedRunbookRepository
{
    public async Task AddAsync(ProposedRunbook runbook, CancellationToken ct = default)
        => await db.ProposedRunbooks.AddAsync(runbook, ct);

    public async Task<ProposedRunbook?> GetByIdAsync(ProposedRunbookId id, CancellationToken ct = default)
        => await db.ProposedRunbooks.FindAsync([id], ct);

    public async Task<IReadOnlyList<ProposedRunbook>> ListAsync(ProposedRunbookStatus? status = null, string? serviceName = null, CancellationToken ct = default)
    {
        var q = db.ProposedRunbooks.AsQueryable();
        if (status.HasValue) q = q.Where(r => r.Status == status.Value);
        if (serviceName is not null) q = q.Where(r => r.ServiceName == serviceName);
        return await q.OrderByDescending(r => r.ProposedAt).ToListAsync(ct);
    }

    public async Task<ProposedRunbook?> GetByIncidentIdAsync(Guid incidentId, CancellationToken ct = default)
        => await db.ProposedRunbooks.FirstOrDefaultAsync(r => r.SourceIncidentId == incidentId, ct);
}
