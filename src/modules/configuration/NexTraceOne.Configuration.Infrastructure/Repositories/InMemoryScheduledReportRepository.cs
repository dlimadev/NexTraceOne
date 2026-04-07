using System.Collections.Concurrent;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Repositories;

/// <summary>
/// Implementação em memória do repositório de relatórios programados.
/// Para uso no MVP1 enquanto a persistência PostgreSQL não for adicionada.
/// </summary>
public sealed class InMemoryScheduledReportRepository : IScheduledReportRepository
{
    private readonly ConcurrentDictionary<Guid, ScheduledReport> _store = new();

    public Task<ScheduledReport?> GetByIdAsync(ScheduledReportId id, string tenantId, CancellationToken cancellationToken)
    {
        _store.TryGetValue(id.Value, out var report);
        var result = report?.TenantId == tenantId ? report : null;
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<ScheduledReport>> ListByTenantAsync(string tenantId, string? userId, CancellationToken cancellationToken)
    {
        IReadOnlyList<ScheduledReport> result = _store.Values
            .Where(r => r.TenantId == tenantId &&
                        (userId is null || r.UserId == userId))
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task AddAsync(ScheduledReport report, CancellationToken cancellationToken)
    {
        _store[report.Id.Value] = report;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(ScheduledReport report, CancellationToken cancellationToken)
    {
        _store[report.Id.Value] = report;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ScheduledReportId id, CancellationToken cancellationToken)
    {
        _store.TryRemove(id.Value, out _);
        return Task.CompletedTask;
    }
}
