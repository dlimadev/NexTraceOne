using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class OnboardingSessionRepository(AiGovernanceDbContext context) : IOnboardingSessionRepository
{
    public async Task AddAsync(OnboardingSession session, CancellationToken ct)
    {
        await context.OnboardingSessions.AddAsync(session, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task<OnboardingSession?> GetByIdAsync(OnboardingSessionId id, CancellationToken ct)
        => await context.OnboardingSessions.SingleOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<OnboardingSession>> ListAsync(Guid? teamId, OnboardingSessionStatus? status, CancellationToken ct)
    {
        var query = context.OnboardingSessions.AsQueryable();

        if (teamId.HasValue)
            query = query.Where(s => s.TeamId == teamId.Value);

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        return await query.OrderByDescending(s => s.StartedAt).ToListAsync(ct);
    }

    public async Task UpdateAsync(OnboardingSession session, CancellationToken ct)
    {
        context.OnboardingSessions.Update(session);
        await context.SaveChangesAsync(ct);
    }
}
