using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class AiSourceRepository(AiGovernanceDbContext context) : IAiSourceRepository
{
    public async Task<AiSource?> GetByIdAsync(AiSourceId id, CancellationToken ct)
        => await context.Sources.SingleOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<AiSource>> GetAllAsync(CancellationToken ct)
        => await context.Sources.OrderBy(s => s.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<AiSource>> GetByTypeAsync(AiSourceType sourceType, CancellationToken ct)
        => await context.Sources
            .Where(s => s.SourceType == sourceType)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AiSource>> GetEnabledAsync(CancellationToken ct)
        => await context.Sources
            .Where(s => s.IsEnabled)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public async Task AddAsync(AiSource entity, CancellationToken ct)
        => await context.Sources.AddAsync(entity, ct);

    public Task UpdateAsync(AiSource entity, CancellationToken ct)
    {
        context.Sources.Update(entity);
        return Task.CompletedTask;
    }
}
