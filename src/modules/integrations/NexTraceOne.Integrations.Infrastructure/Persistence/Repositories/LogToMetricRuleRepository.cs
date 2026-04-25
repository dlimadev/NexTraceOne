using Microsoft.EntityFrameworkCore;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Integrations.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de regras log → métrica.
/// </summary>
internal sealed class LogToMetricRuleRepository(IntegrationsDbContext context) : ILogToMetricRuleRepository
{
    public async Task<(IReadOnlyList<LogToMetricRule> Items, int TotalCount)> ListAsync(
        bool? isEnabled,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = context.LogToMetricRules.AsQueryable();
        if (isEnabled.HasValue)
            query = query.Where(r => r.IsEnabled == isEnabled.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<LogToMetricRule>> ListEnabledAsync(CancellationToken ct)
        => await context.LogToMetricRules
            .Where(r => r.IsEnabled)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<LogToMetricRule?> GetByIdAsync(LogToMetricRuleId id, CancellationToken ct)
        => await context.LogToMetricRules.SingleOrDefaultAsync(r => r.Id == id, ct);

    public async Task AddAsync(LogToMetricRule rule, CancellationToken ct)
        => await context.LogToMetricRules.AddAsync(rule, ct);

    public Task UpdateAsync(LogToMetricRule rule, CancellationToken ct)
    {
        context.LogToMetricRules.Update(rule);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(LogToMetricRule rule, CancellationToken ct)
    {
        context.LogToMetricRules.Remove(rule);
        return Task.CompletedTask;
    }
}
