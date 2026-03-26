using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;

/// <summary>
/// Repositório para persistência e consulta de SloDefinition.
/// </summary>
public interface ISloDefinitionRepository
{
    Task<SloDefinition?> GetByIdAsync(SloDefinitionId id, Guid tenantId, CancellationToken ct);
    Task<IReadOnlyList<SloDefinition>> GetByServiceAsync(string serviceId, Guid tenantId, CancellationToken ct);
    Task<IReadOnlyList<SloDefinition>> GetActiveByServiceAsync(string serviceId, string environment, Guid tenantId, CancellationToken ct);
    Task AddAsync(SloDefinition slo, CancellationToken ct);
}
