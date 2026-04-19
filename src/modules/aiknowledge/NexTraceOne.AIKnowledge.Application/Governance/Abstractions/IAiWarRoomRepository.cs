using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

public interface IAiWarRoomRepository
{
    Task<WarRoomSession?> GetByIdAsync(WarRoomSessionId id, CancellationToken ct);
    Task<IReadOnlyList<WarRoomSession>> ListByIncidentAsync(string incidentId, Guid tenantId, CancellationToken ct);
    Task<IReadOnlyList<WarRoomSession>> ListOpenAsync(Guid tenantId, CancellationToken ct);
    Task<IReadOnlyList<WarRoomSession>> ListByTenantAsync(Guid tenantId, CancellationToken ct);
    void Add(WarRoomSession session);
}
