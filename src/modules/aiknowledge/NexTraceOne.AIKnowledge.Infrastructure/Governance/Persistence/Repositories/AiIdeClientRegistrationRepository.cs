using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class AiIdeClientRegistrationRepository(AiGovernanceDbContext context) : IAiIdeClientRegistrationRepository
{
    public async Task<AIIDEClientRegistration?> GetByIdAsync(AIIDEClientRegistrationId id, CancellationToken cancellationToken)
        => await context.IdeClientRegistrations.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AIIDEClientRegistration>> ListAsync(
        string? userId, AIClientType? clientType, bool? isActive, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.IdeClientRegistrations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(r => r.UserId == userId);

        if (clientType.HasValue)
            query = query.Where(r => r.ClientType == clientType.Value);

        if (isActive.HasValue)
            query = query.Where(r => r.IsActive == isActive.Value);

        return await query.Take(pageSize).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AIIDEClientRegistration registration, CancellationToken cancellationToken)
        => await context.IdeClientRegistrations.AddAsync(registration, cancellationToken);

    public Task UpdateAsync(AIIDEClientRegistration registration, CancellationToken cancellationToken)
    {
        context.IdeClientRegistrations.Update(registration);
        return Task.CompletedTask;
    }
}
