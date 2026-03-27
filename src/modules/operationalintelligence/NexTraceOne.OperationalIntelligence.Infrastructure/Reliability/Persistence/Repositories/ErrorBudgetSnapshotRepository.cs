using Microsoft.EntityFrameworkCore;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence.Repositories;

/// <summary>Repositório EF Core para ErrorBudgetSnapshot.</summary>
internal sealed class ErrorBudgetSnapshotRepository(ReliabilityDbContext context) : IErrorBudgetSnapshotRepository
{
    public async Task<ErrorBudgetSnapshot?> GetLatestAsync(SloDefinitionId sloId, Guid tenantId, CancellationToken ct)
        => await context.ErrorBudgetSnapshots
            .AsNoTracking()
            .Where(s => s.SloDefinitionId == sloId && s.TenantId == tenantId)
            .OrderByDescending(s => s.ComputedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<ErrorBudgetSnapshot>> GetHistoryAsync(SloDefinitionId sloId, Guid tenantId, int maxCount, CancellationToken ct)
        => await context.ErrorBudgetSnapshots
            .AsNoTracking()
            .Where(s => s.SloDefinitionId == sloId && s.TenantId == tenantId)
            .OrderByDescending(s => s.ComputedAt)
            .Take(maxCount)
            .ToListAsync(ct);

    public async Task AddAsync(ErrorBudgetSnapshot snapshot, CancellationToken ct)
    {
        await context.ErrorBudgetSnapshots.AddAsync(snapshot, ct);
        await context.CommitAsync(ct);
    }
}
