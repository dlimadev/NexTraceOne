using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de execuções de avaliação de modelos IA (CC-05).
/// </summary>
public interface IAiEvalRunRepository
{
    Task<AiEvalRun?> GetByIdAsync(AiEvalRunId id, CancellationToken ct = default);
    Task<IReadOnlyList<AiEvalRun>> ListByDatasetAsync(Guid datasetId, string tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<AiEvalRun>> ListByModelAsync(string modelId, string tenantId, CancellationToken ct = default);
    Task AddAsync(AiEvalRun run, CancellationToken ct = default);
    Task UpdateAsync(AiEvalRun run, CancellationToken ct = default);
}
