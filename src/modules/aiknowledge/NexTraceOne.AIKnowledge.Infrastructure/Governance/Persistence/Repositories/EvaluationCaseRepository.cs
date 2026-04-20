using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class EvaluationCaseRepository(AiGovernanceDbContext context) : IEvaluationCaseRepository
{
    public void Add(EvaluationCase evalCase)
        => context.EvaluationCases.Add(evalCase);

    public async Task<IReadOnlyList<EvaluationCase>> ListBySuiteAsync(EvaluationSuiteId suiteId, CancellationToken ct)
        => await context.EvaluationCases
            .Where(c => c.SuiteId == suiteId && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
}
