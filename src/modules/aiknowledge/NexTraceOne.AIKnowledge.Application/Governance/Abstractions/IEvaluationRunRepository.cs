using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de execuções de avaliação do AI Evaluation Harness.
/// </summary>
public interface IEvaluationRunRepository
{
    /// <summary>Adiciona uma nova execução de avaliação para persistência.</summary>
    void Add(EvaluationRun run);

    /// <summary>Obtém uma execução pelo identificador.</summary>
    Task<EvaluationRun?> GetByIdAsync(EvaluationRunId id, CancellationToken ct = default);

    /// <summary>Lista execuções de uma suite de avaliação.</summary>
    Task<IReadOnlyList<EvaluationRun>> ListBySuiteAsync(EvaluationSuiteId suiteId, CancellationToken ct = default);
}
