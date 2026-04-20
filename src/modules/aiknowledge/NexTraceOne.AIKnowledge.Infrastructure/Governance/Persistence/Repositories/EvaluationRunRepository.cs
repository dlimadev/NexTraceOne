using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class EvaluationRunRepository(AiGovernanceDbContext context) : IEvaluationRunRepository
{
    public void Add(EvaluationRun run)
        => context.EvaluationRuns.Add(run);

    public async Task<EvaluationRun?> GetByIdAsync(EvaluationRunId id, CancellationToken ct)
        => await context.EvaluationRuns.SingleOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<EvaluationRun>> ListBySuiteAsync(EvaluationSuiteId suiteId, CancellationToken ct)
        => await context.EvaluationRuns
            .Where(r => r.SuiteId == suiteId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
}
