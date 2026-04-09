using Microsoft.EntityFrameworkCore;

using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de ServiceMaturityAssessment usando EF Core.
/// </summary>
internal sealed class ServiceMaturityAssessmentRepository(GovernanceDbContext context)
    : IServiceMaturityAssessmentRepository
{
    public async Task<ServiceMaturityAssessment?> GetByIdAsync(
        ServiceMaturityAssessmentId id, CancellationToken ct)
        => await context.ServiceMaturityAssessments.SingleOrDefaultAsync(a => a.Id == id, ct);

    public async Task<ServiceMaturityAssessment?> GetByServiceIdAsync(
        Guid serviceId, CancellationToken ct)
        => await context.ServiceMaturityAssessments.SingleOrDefaultAsync(a => a.ServiceId == serviceId, ct);

    public async Task<IReadOnlyList<ServiceMaturityAssessment>> ListAsync(
        ServiceMaturityLevel? level, CancellationToken ct)
    {
        var query = context.ServiceMaturityAssessments.AsQueryable();

        if (level.HasValue)
            query = query.Where(a => a.CurrentLevel == level.Value);

        return await query
            .OrderBy(a => a.ServiceName)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task AddAsync(ServiceMaturityAssessment assessment, CancellationToken ct)
        => await context.ServiceMaturityAssessments.AddAsync(assessment, ct);

    public Task UpdateAsync(ServiceMaturityAssessment assessment, CancellationToken ct)
    {
        context.ServiceMaturityAssessments.Update(assessment);
        return Task.CompletedTask;
    }
}
