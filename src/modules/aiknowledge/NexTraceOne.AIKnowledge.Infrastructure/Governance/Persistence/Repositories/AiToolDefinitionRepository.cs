using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class AiToolDefinitionRepository(AiGovernanceDbContext context) : IAiToolDefinitionRepository
{
    public async Task<AiToolDefinition?> GetByIdAsync(AiToolDefinitionId id, CancellationToken ct)
        => await context.ToolDefinitions.SingleOrDefaultAsync(t => t.Id == id, ct);

    public async Task<AiToolDefinition?> GetByNameAsync(string name, CancellationToken ct)
        => await context.ToolDefinitions.SingleOrDefaultAsync(t => t.Name == name, ct);

    public async Task<IReadOnlyList<AiToolDefinition>> GetAllActiveAsync(CancellationToken ct)
        => await context.ToolDefinitions.Where(t => t.IsActive).OrderBy(t => t.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<AiToolDefinition>> GetByCategoryAsync(string category, CancellationToken ct)
    {
        var query = context.ToolDefinitions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(t => t.Category == category);

        return await query.OrderBy(t => t.Name).ToListAsync(ct);
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct)
        => await context.ToolDefinitions.AnyAsync(t => t.Name == name, ct);

    public async Task AddAsync(AiToolDefinition entity, CancellationToken ct)
    {
        await context.ToolDefinitions.AddAsync(entity, ct);
        await context.SaveChangesAsync(ct);
    }
}
