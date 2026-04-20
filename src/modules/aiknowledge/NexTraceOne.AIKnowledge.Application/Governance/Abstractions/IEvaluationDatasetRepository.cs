using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de datasets de avaliação do AI Evaluation Harness.
/// </summary>
public interface IEvaluationDatasetRepository
{
    /// <summary>Adiciona um novo dataset para persistência.</summary>
    void Add(EvaluationDataset dataset);

    /// <summary>Obtém um dataset pelo identificador.</summary>
    Task<EvaluationDataset?> GetByIdAsync(EvaluationDatasetId id, CancellationToken ct = default);

    /// <summary>Lista datasets de um tenant.</summary>
    Task<IReadOnlyList<EvaluationDataset>> ListByTenantAsync(Guid tenantId, CancellationToken ct = default);
}
