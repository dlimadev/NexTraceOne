using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class IdeQuerySessionRepository(AiGovernanceDbContext context) : IIdeQuerySessionRepository
{
    public async Task AddAsync(IdeQuerySession session, CancellationToken ct)
    {
        await context.IdeQuerySessions.AddAsync(session, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task<IdeQuerySession?> GetByIdAsync(IdeQuerySessionId id, CancellationToken ct)
        => await context.IdeQuerySessions.SingleOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<IdeQuerySession>> ListAsync(
        string? userId, string? ideClient, IdeQuerySessionStatus? status, CancellationToken ct)
    {
        var query = context.IdeQuerySessions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(s => s.UserId == userId);

        if (!string.IsNullOrWhiteSpace(ideClient))
            query = query.Where(s => s.IdeClient == ideClient);

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        return await query.OrderByDescending(s => s.SubmittedAt).ToListAsync(ct);
    }

    public async Task UpdateAsync(IdeQuerySession session, CancellationToken ct)
    {
        context.IdeQuerySessions.Update(session);
        await context.SaveChangesAsync(ct);
    }
}
