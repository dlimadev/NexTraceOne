using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de casos de avaliação do AI Evaluation Harness.
/// </summary>
public interface IEvaluationCaseRepository
{
    /// <summary>Adiciona um novo caso de avaliação para persistência.</summary>
    void Add(EvaluationCase evalCase);

    /// <summary>Lista casos de uma suite de avaliação.</summary>
    Task<IReadOnlyList<EvaluationCase>> ListBySuiteAsync(EvaluationSuiteId suiteId, CancellationToken ct = default);
}
