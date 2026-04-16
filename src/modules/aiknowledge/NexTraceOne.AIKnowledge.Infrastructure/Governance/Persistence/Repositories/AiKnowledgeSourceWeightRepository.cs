using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class AiKnowledgeSourceWeightRepository(AiGovernanceDbContext context) : IAiKnowledgeSourceWeightRepository
{
    public async Task<IReadOnlyList<AIKnowledgeSourceWeight>> ListAsync(
        AIUseCaseType? useCaseType,
        bool? isActive,
        CancellationToken ct)
    {
        var query = context.SourceWeights.AsQueryable();

        if (useCaseType.HasValue)
            query = query.Where(w => w.UseCaseType == useCaseType.Value);

        if (isActive.HasValue)
            query = query.Where(w => w.IsActive == isActive.Value);

        return await query
            .OrderBy(w => w.UseCaseType)
            .ThenByDescending(w => w.Weight)
            .ToListAsync(ct);
    }

    public async Task AddAsync(AIKnowledgeSourceWeight weight, CancellationToken ct)
        => await context.SourceWeights.AddAsync(weight, ct);

    public Task UpdateAsync(AIKnowledgeSourceWeight weight, CancellationToken ct)
    {
        context.SourceWeights.Update(weight);
        return Task.CompletedTask;
    }
}
