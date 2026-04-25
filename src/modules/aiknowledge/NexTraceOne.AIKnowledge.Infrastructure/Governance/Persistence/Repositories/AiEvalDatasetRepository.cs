using Microsoft.EntityFrameworkCore;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class AiEvalDatasetRepository(AiGovernanceDbContext context) : IAiEvalDatasetRepository
{
    public async Task<AiEvalDataset?> GetByIdAsync(AiEvalDatasetId id, CancellationToken ct = default)
        => await context.EvalDatasets.SingleOrDefaultAsync(d => d.Id == id, ct);

    public async Task<IReadOnlyList<AiEvalDataset>> ListByTenantAsync(string tenantId, CancellationToken ct = default)
        => await context.EvalDatasets
            .Where(d => d.TenantId == tenantId && d.IsActive)
            .OrderByDescending(d => d.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task AddAsync(AiEvalDataset dataset, CancellationToken ct = default)
        => await context.EvalDatasets.AddAsync(dataset, ct);

    public Task UpdateAsync(AiEvalDataset dataset, CancellationToken ct = default)
    {
        context.EvalDatasets.Update(dataset);
        return Task.CompletedTask;
    }
}
