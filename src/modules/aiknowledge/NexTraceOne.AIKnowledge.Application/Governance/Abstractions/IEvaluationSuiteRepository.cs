using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de suites de avaliação do AI Evaluation Harness.
/// </summary>
public interface IEvaluationSuiteRepository
{
    /// <summary>Adiciona uma nova suite para persistência.</summary>
    void Add(EvaluationSuite suite);

    /// <summary>Obtém uma suite pelo identificador.</summary>
    Task<EvaluationSuite?> GetByIdAsync(EvaluationSuiteId id, CancellationToken ct = default);

    /// <summary>Lista suites de um tenant com filtro opcional por caso de uso.</summary>
    Task<IReadOnlyList<EvaluationSuite>> ListByTenantAsync(Guid tenantId, string? useCase, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Conta suites de um tenant.</summary>
    Task<int> CountByTenantAsync(Guid tenantId, string? useCase, CancellationToken ct = default);
}
