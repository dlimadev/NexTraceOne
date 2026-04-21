using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>Repositório de registos de auditoria de queries de agentes autónomos (Wave D.4).</summary>
public interface IAgentQueryRepository
{
    Task AddAsync(AgentQueryRecord record, CancellationToken ct);
    Task<IReadOnlyList<AgentQueryRecord>> ListByTokenAsync(Guid tokenId, int limit, CancellationToken ct);
    Task<IReadOnlyList<AgentQueryRecord>> ListByTenantAsync(Guid tenantId, DateTimeOffset since, int limit, CancellationToken ct);
}
