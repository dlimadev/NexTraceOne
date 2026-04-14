using Microsoft.EntityFrameworkCore;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Repositories;

internal sealed class AutomationRuleRepository(ConfigurationDbContext context) : IAutomationRuleRepository
{
    public async Task<AutomationRule?> GetByIdAsync(AutomationRuleId id, string tenantId, CancellationToken cancellationToken)
        => await context.AutomationRules.SingleOrDefaultAsync(
            r => r.Id == id && r.TenantId == tenantId, cancellationToken);

    public async Task<IReadOnlyList<AutomationRule>> ListByTenantAsync(string tenantId, CancellationToken cancellationToken)
        => await context.AutomationRules
            .Where(r => r.TenantId == tenantId)
            .OrderBy(r => r.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AutomationRule>> GetByTriggerAsync(string tenantId, string trigger, CancellationToken cancellationToken)
        => await context.AutomationRules
            .Where(r => r.TenantId == tenantId && r.Trigger == trigger && r.IsEnabled)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task AddAsync(AutomationRule rule, CancellationToken cancellationToken)
    {
        await context.AutomationRules.AddAsync(rule, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AutomationRule rule, CancellationToken cancellationToken)
    {
        context.AutomationRules.Update(rule);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(AutomationRuleId id, CancellationToken cancellationToken)
    {
        var entity = await context.AutomationRules.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (entity is not null)
        {
            context.AutomationRules.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
