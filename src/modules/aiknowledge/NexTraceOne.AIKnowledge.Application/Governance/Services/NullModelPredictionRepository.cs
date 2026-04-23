using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Services;

/// <summary>
/// Implementação null (honest-null) de IModelPredictionRepository.
/// Não persiste dados — serve como bridge sem infra real.
/// Wave AT.1 — IngestModelPredictionSample.
/// </summary>
public sealed class NullModelPredictionRepository : IModelPredictionRepository
{
    public Task AddAsync(ModelPredictionSample sample, CancellationToken ct)
        => Task.CompletedTask;

    public Task<IReadOnlyList<ModelPredictionSample>> ListByModelAsync(
        Guid modelId, string tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<ModelPredictionSample>>([]);

    public Task<IReadOnlyList<ModelPredictionSample>> ListByTenantAsync(
        string tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<ModelPredictionSample>>([]);

    public Task<int> CountByModelAsync(Guid modelId, string tenantId, DateTimeOffset from, CancellationToken ct)
        => Task.FromResult(0);
}
