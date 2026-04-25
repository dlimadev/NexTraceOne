using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de datasets de avaliação de modelos IA (CC-05).
/// </summary>
public interface IAiEvalDatasetRepository
{
    Task<AiEvalDataset?> GetByIdAsync(AiEvalDatasetId id, CancellationToken ct = default);
    Task<IReadOnlyList<AiEvalDataset>> ListByTenantAsync(string tenantId, CancellationToken ct = default);
    Task AddAsync(AiEvalDataset dataset, CancellationToken ct = default);
    Task UpdateAsync(AiEvalDataset dataset, CancellationToken ct = default);
}
