using Microsoft.EntityFrameworkCore;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class AiEvalRunRepository(AiGovernanceDbContext context) : IAiEvalRunRepository
{
    public async Task<AiEvalRun?> GetByIdAsync(AiEvalRunId id, CancellationToken ct = default)
        => await context.EvalRuns.SingleOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<AiEvalRun>> ListByDatasetAsync(Guid datasetId, string tenantId, CancellationToken ct = default)
        => await context.EvalRuns
            .Where(r => r.DatasetId == datasetId && r.TenantId == tenantId)
            .OrderByDescending(r => r.StartedAt)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AiEvalRun>> ListByModelAsync(string modelId, string tenantId, CancellationToken ct = default)
        => await context.EvalRuns
            .Where(r => r.ModelId == modelId && r.TenantId == tenantId)
            .OrderByDescending(r => r.StartedAt)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task AddAsync(AiEvalRun run, CancellationToken ct = default)
        => await context.EvalRuns.AddAsync(run, ct);

    public Task UpdateAsync(AiEvalRun run, CancellationToken ct = default)
    {
        context.EvalRuns.Update(run);
        return Task.CompletedTask;
    }
}
