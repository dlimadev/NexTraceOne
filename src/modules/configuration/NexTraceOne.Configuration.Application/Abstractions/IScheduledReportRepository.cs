using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Abstractions;

/// <summary>Contrato do repositório de relatórios programados.</summary>
public interface IScheduledReportRepository
{
    Task<ScheduledReport?> GetByIdAsync(ScheduledReportId id, string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ScheduledReport>> ListByTenantAsync(string tenantId, string? userId, CancellationToken cancellationToken);
    Task AddAsync(ScheduledReport report, CancellationToken cancellationToken);
    Task UpdateAsync(ScheduledReport report, CancellationToken cancellationToken);
    Task DeleteAsync(ScheduledReportId id, CancellationToken cancellationToken);
}
