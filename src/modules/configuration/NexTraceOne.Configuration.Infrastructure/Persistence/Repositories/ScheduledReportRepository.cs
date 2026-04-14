using Microsoft.EntityFrameworkCore;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Repositories;

internal sealed class ScheduledReportRepository(ConfigurationDbContext context) : IScheduledReportRepository
{
    public async Task<ScheduledReport?> GetByIdAsync(ScheduledReportId id, string tenantId, CancellationToken cancellationToken)
        => await context.ScheduledReports.SingleOrDefaultAsync(
            r => r.Id == id && r.TenantId == tenantId, cancellationToken);

    public async Task<IReadOnlyList<ScheduledReport>> ListByTenantAsync(string tenantId, string? userId, CancellationToken cancellationToken)
        => await context.ScheduledReports
            .Where(r => r.TenantId == tenantId
                && (userId == null || r.UserId == userId))
            .OrderBy(r => r.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ScheduledReport report, CancellationToken cancellationToken)
    {
        await context.ScheduledReports.AddAsync(report, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ScheduledReport report, CancellationToken cancellationToken)
    {
        context.ScheduledReports.Update(report);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ScheduledReportId id, CancellationToken cancellationToken)
    {
        var entity = await context.ScheduledReports.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (entity is not null)
        {
            context.ScheduledReports.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
