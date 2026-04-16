using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class AiModelRepository(AiGovernanceDbContext context) : IAiModelRepository
{
    public async Task<IReadOnlyList<AIModel>> ListAsync(
        string? provider, ModelType? modelType, ModelStatus? status, bool? isInternal, CancellationToken ct)
    {
        var query = context.Models.AsQueryable();

        if (!string.IsNullOrWhiteSpace(provider))
            query = query.Where(m => m.Provider == provider);

        if (modelType.HasValue)
            query = query.Where(m => m.ModelType == modelType.Value);

        if (status.HasValue)
            query = query.Where(m => m.Status == status.Value);

        if (isInternal.HasValue)
            query = query.Where(m => m.IsInternal == isInternal.Value);

        return await query.OrderBy(m => m.Name).ToListAsync(ct);
    }

    public async Task<AIModel?> GetByIdAsync(AIModelId id, CancellationToken ct)
        => await context.Models.SingleOrDefaultAsync(m => m.Id == id, ct);

    public async Task AddAsync(AIModel model, CancellationToken ct)
        => await context.Models.AddAsync(model, ct);

    public Task UpdateAsync(AIModel model, CancellationToken ct)
    {
        context.Models.Update(model);
        return Task.CompletedTask;
    }
}
