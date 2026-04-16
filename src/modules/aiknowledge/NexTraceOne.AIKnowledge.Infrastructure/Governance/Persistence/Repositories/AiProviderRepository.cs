using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class AiProviderRepository(AiGovernanceDbContext context) : IAiProviderRepository
{
    public async Task<AiProvider?> GetByIdAsync(AiProviderId id, CancellationToken ct)
        => await context.Providers.SingleOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<AiProvider>> GetAllAsync(CancellationToken ct)
        => await context.Providers.OrderBy(p => p.Priority).ToListAsync(ct);

    public async Task<IReadOnlyList<AiProvider>> GetEnabledAsync(CancellationToken ct)
        => await context.Providers
            .Where(p => p.IsEnabled)
            .OrderBy(p => p.Priority)
            .ToListAsync(ct);

    public async Task AddAsync(AiProvider entity, CancellationToken ct)
        => await context.Providers.AddAsync(entity, ct);
}
