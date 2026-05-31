using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

/// <summary>
/// Implementação em memória do repositório de predições de modelo.
/// Usada exclusivamente em testes unitários.
/// </summary>
public sealed class TestModelPredictionRepository : IModelPredictionRepository
{
    private readonly List<ModelPredictionSample> _store = [];

    public Task AddAsync(ModelPredictionSample sample, CancellationToken ct)
    {
        _store.Add(sample);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ModelPredictionSample>> ListByModelAsync(
        Guid modelId, string tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        IReadOnlyList<ModelPredictionSample> result = _store
            .Where(s => s.ModelId == modelId && s.TenantId == tenantId && s.CreatedAt >= from && s.CreatedAt <= to)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<ModelPredictionSample>> ListByTenantAsync(
        string tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        IReadOnlyList<ModelPredictionSample> result = _store
            .Where(s => s.TenantId == tenantId && s.CreatedAt >= from && s.CreatedAt <= to)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<int> CountByModelAsync(Guid modelId, string tenantId, DateTimeOffset from, CancellationToken ct)
    {
        var count = _store
            .Count(s => s.ModelId == modelId && s.TenantId == tenantId && s.CreatedAt >= from);
        return Task.FromResult(count);
    }
}
