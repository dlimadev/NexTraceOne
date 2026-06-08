using Microsoft.EntityFrameworkCore;

using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de ServiceMaturityHistory usando EF Core.
/// Append-only — sem operações de actualização.
/// </summary>
internal sealed class ServiceMaturityHistoryRepository(PlatformGovernanceDbContext context)
    : IServiceMaturityHistoryRepository
{
    public async Task<IReadOnlyList<ServiceMaturityHistory>> ListByServiceIdAsync(
        Guid serviceId,
        CancellationToken ct)
        => await context.ServiceMaturityHistory
            .Where(h => h.ServiceId == serviceId)
            .OrderBy(h => h.RecordedAt)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ServiceMaturityHistory>> ListByAssessmentIdAsync(
        ServiceMaturityAssessmentId assessmentId,
        CancellationToken ct)
        => await context.ServiceMaturityHistory
            .Where(h => h.AssessmentId == assessmentId)
            .OrderBy(h => h.RecordedAt)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task AddAsync(ServiceMaturityHistory history, CancellationToken ct)
        => await context.ServiceMaturityHistory.AddAsync(history, ct);
}
