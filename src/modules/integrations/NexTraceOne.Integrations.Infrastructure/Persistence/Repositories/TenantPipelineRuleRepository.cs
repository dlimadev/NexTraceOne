using Microsoft.EntityFrameworkCore;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de regras de pipeline por tenant.
/// </summary>
internal sealed class TenantPipelineRuleRepository(IntegrationsDbContext context) : ITenantPipelineRuleRepository
{
    public async Task<(IReadOnlyList<TenantPipelineRule> Items, int TotalCount)> ListAsync(
        PipelineRuleType? ruleType,
        PipelineSignalType? signalType,
        bool? isEnabled,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = context.TenantPipelineRules.AsQueryable();

        if (ruleType.HasValue)
            query = query.Where(r => r.RuleType == ruleType.Value);

        if (signalType.HasValue)
            query = query.Where(r => r.SignalType == signalType.Value);

        if (isEnabled.HasValue)
            query = query.Where(r => r.IsEnabled == isEnabled.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(r => r.Priority)
            .ThenByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<TenantPipelineRule>> ListEnabledBySignalTypeAsync(
        PipelineSignalType signalType,
        CancellationToken ct)
        => await context.TenantPipelineRules
            .Where(r => r.IsEnabled && r.SignalType == signalType)
            .OrderBy(r => r.Priority)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<TenantPipelineRule?> GetByIdAsync(TenantPipelineRuleId id, CancellationToken ct)
        => await context.TenantPipelineRules.SingleOrDefaultAsync(r => r.Id == id, ct);

    public async Task AddAsync(TenantPipelineRule rule, CancellationToken ct)
        => await context.TenantPipelineRules.AddAsync(rule, ct);

    public Task UpdateAsync(TenantPipelineRule rule, CancellationToken ct)
    {
        context.TenantPipelineRules.Update(rule);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TenantPipelineRule rule, CancellationToken ct)
    {
        context.TenantPipelineRules.Remove(rule);
        return Task.CompletedTask;
    }
}
