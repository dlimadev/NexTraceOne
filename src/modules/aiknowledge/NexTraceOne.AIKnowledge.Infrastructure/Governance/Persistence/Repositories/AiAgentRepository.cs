using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class AiAgentRepository(AiGovernanceDbContext context) : IAiAgentRepository
{
    public async Task<IReadOnlyList<AiAgent>> ListAsync(bool? isActive, bool? isOfficial, CancellationToken ct)
    {
        var query = context.Agents.AsQueryable();

        if (isActive.HasValue)
            query = query.Where(a => a.IsActive == isActive.Value);

        if (isOfficial.HasValue)
            query = query.Where(a => a.IsOfficial == isOfficial.Value);

        return await query.OrderBy(a => a.SortOrder).ThenBy(a => a.DisplayName).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AiAgent>> ListByCategoriesAsync(
        IReadOnlyList<AgentCategory> categories,
        bool? isActive,
        CancellationToken ct)
    {
        var query = context.Agents.Where(a => categories.Contains(a.Category));

        if (isActive.HasValue)
            query = query.Where(a => a.IsActive == isActive.Value);

        return await query.OrderBy(a => a.SortOrder).ThenBy(a => a.DisplayName).ToListAsync(ct);
    }

    public async Task<AiAgent?> GetByIdAsync(AiAgentId id, CancellationToken ct)
        => await context.Agents.SingleOrDefaultAsync(a => a.Id == id, ct);

    public async Task AddAsync(AiAgent agent, CancellationToken ct)
        => await context.Agents.AddAsync(agent, ct);

    public Task UpdateAsync(AiAgent agent, CancellationToken ct)
    {
        context.Agents.Update(agent);
        return Task.CompletedTask;
    }

    public async Task<AiAgent?> GetBySlugAsync(string slug, CancellationToken ct)
        => await context.Agents.SingleOrDefaultAsync(a => a.Slug == slug, ct);

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct)
        => await context.Agents.AnyAsync(a => a.Name == name, ct);
}
