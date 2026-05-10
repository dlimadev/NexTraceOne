using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class EfModelPredictionRepository(AiGovernanceDbContext context) : IModelPredictionRepository
{
    public async Task AddAsync(ModelPredictionSample sample, CancellationToken ct)
        => await context.ModelPredictionSamples.AddAsync(sample, ct);

    public async Task<IReadOnlyList<ModelPredictionSample>> ListByModelAsync(
        Guid modelId, string tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
        => await context.ModelPredictionSamples
            .Where(s => s.ModelId == modelId && s.TenantId == tenantId
                     && s.PredictedAt >= from && s.PredictedAt <= to)
            .OrderByDescending(s => s.PredictedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ModelPredictionSample>> ListByTenantAsync(
        string tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
        => await context.ModelPredictionSamples
            .Where(s => s.TenantId == tenantId && s.PredictedAt >= from && s.PredictedAt <= to)
            .OrderByDescending(s => s.PredictedAt)
            .ToListAsync(ct);

    public async Task<int> CountByModelAsync(
        Guid modelId, string tenantId, DateTimeOffset from, CancellationToken ct)
        => await context.ModelPredictionSamples
            .CountAsync(s => s.ModelId == modelId && s.TenantId == tenantId && s.PredictedAt >= from, ct);
}
