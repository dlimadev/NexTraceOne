using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;

/// <summary>
/// Repositório para persistência e consulta de SlaDefinition.
/// </summary>
public interface ISlaDefinitionRepository
{
    Task<SlaDefinition?> GetByIdAsync(SlaDefinitionId id, Guid tenantId, CancellationToken ct);
    Task<IReadOnlyList<SlaDefinition>> GetBySloAsync(SloDefinitionId sloId, Guid tenantId, CancellationToken ct);
    Task AddAsync(SlaDefinition sla, CancellationToken ct);
}
