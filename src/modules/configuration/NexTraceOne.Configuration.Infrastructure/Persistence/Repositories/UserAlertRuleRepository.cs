using Microsoft.EntityFrameworkCore;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Repositories;

internal sealed class UserAlertRuleRepository(ConfigurationDbContext context) : IUserAlertRuleRepository
{
    public async Task<UserAlertRule?> GetByIdAsync(UserAlertRuleId id, CancellationToken cancellationToken)
        => await context.UserAlertRules.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<UserAlertRule>> ListByUserAsync(string userId, string tenantId, CancellationToken cancellationToken)
        => await context.UserAlertRules
            .Where(r => r.UserId == userId && r.TenantId == tenantId)
            .OrderByDescending(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task AddAsync(UserAlertRule rule, CancellationToken cancellationToken)
    {
        await context.UserAlertRules.AddAsync(rule, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UserAlertRule rule, CancellationToken cancellationToken)
    {
        context.UserAlertRules.Update(rule);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(UserAlertRule rule, CancellationToken cancellationToken)
    {
        context.UserAlertRules.Remove(rule);
        await context.SaveChangesAsync(cancellationToken);
    }
}
