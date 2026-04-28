using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>Repositório de registos de agentes NexTrace.</summary>
public interface IAgentRegistrationRepository
{
    Task<AgentRegistration?> GetByHostUnitIdAsync(Guid tenantId, Guid hostUnitId, CancellationToken ct = default);
    Task<IReadOnlyList<AgentRegistration>> ListByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<decimal> SumActiveHostUnitsAsync(Guid tenantId, CancellationToken ct = default);
    void Add(AgentRegistration registration);
    void Update(AgentRegistration registration);
}
