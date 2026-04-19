using Microsoft.EntityFrameworkCore;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class WarRoomRepository(AiGovernanceDbContext context) : IAiWarRoomRepository
{
    public async Task<WarRoomSession?> GetByIdAsync(WarRoomSessionId id, CancellationToken ct)
        => await context.WarRooms.SingleOrDefaultAsync(w => w.Id == id, ct);

    public async Task<IReadOnlyList<WarRoomSession>> ListByIncidentAsync(string incidentId, Guid tenantId, CancellationToken ct)
        => await context.WarRooms
            .Where(w => w.IncidentId == incidentId && w.TenantId == tenantId)
            .OrderByDescending(w => w.OpenedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<WarRoomSession>> ListOpenAsync(Guid tenantId, CancellationToken ct)
        => await context.WarRooms
            .Where(w => w.TenantId == tenantId && (w.Status == "Open" || w.Status == "Active"))
            .OrderByDescending(w => w.OpenedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<WarRoomSession>> ListByTenantAsync(Guid tenantId, CancellationToken ct)
        => await context.WarRooms
            .Where(w => w.TenantId == tenantId)
            .OrderByDescending(w => w.OpenedAt)
            .ToListAsync(ct);

    public void Add(WarRoomSession session) => context.WarRooms.Add(session);
}
