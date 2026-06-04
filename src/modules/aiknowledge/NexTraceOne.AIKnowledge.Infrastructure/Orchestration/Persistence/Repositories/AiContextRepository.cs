using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Orchestration.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;
using NexTraceOne.AIKnowledge.Infrastructure.Persistence;

namespace NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Persistence.Repositories;

internal sealed class AiContextRepository(AiHubDbContext context) : IAiContextRepository
{
    public async Task<AiContext?> GetByIdAsync(AiContextId id, CancellationToken ct)
        => await context.Contexts.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddAsync(AiContext entity, CancellationToken ct)
    {
        await context.Contexts.AddAsync(entity, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AiContext>> GetRecentByServiceAsync(
        string serviceName,
        int maxCount,
        CancellationToken ct)
    {
        return await context.Contexts
            .Where(c => c.ServiceName == serviceName)
            .OrderByDescending(c => c.CreatedAt)
            .Take(maxCount)
            .ToListAsync(ct);
    }
}
