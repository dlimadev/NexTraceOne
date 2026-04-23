using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de <see cref="ModelPredictionSample"/> — persiste e consulta amostras
/// de predição de modelos de IA para detecção de drift e análise de qualidade.
/// Por omissão satisfeita por <c>NullModelPredictionRepository</c> (honest-null).
/// Wave AT.1 — AI Model Quality &amp; Drift Governance.
/// </summary>
public interface IModelPredictionRepository
{
    /// <summary>Persiste uma nova amostra de predição.</summary>
    Task AddAsync(ModelPredictionSample sample, CancellationToken ct);

    /// <summary>Lista amostras de um modelo num período específico.</summary>
    Task<IReadOnlyList<ModelPredictionSample>> ListByModelAsync(
        Guid modelId,
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);

    /// <summary>Lista amostras de todos os modelos de um tenant num período.</summary>
    Task<IReadOnlyList<ModelPredictionSample>> ListByTenantAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);

    /// <summary>Conta amostras de um modelo num período.</summary>
    Task<int> CountByModelAsync(Guid modelId, string tenantId, DateTimeOffset from, CancellationToken ct);
}
