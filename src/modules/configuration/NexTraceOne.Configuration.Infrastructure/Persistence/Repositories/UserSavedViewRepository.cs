using Microsoft.EntityFrameworkCore;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Repositories;

internal sealed class UserSavedViewRepository(ConfigurationDbContext context) : IUserSavedViewRepository
{
    public async Task<UserSavedView?> GetByIdAsync(UserSavedViewId id, CancellationToken ct)
        => await context.UserSavedViews.SingleOrDefaultAsync(v => v.Id == id, ct);

    public async Task<IReadOnlyList<UserSavedView>> ListByUserAsync(string userId, string? context_, CancellationToken ct)
        => await context.UserSavedViews
            .Where(v => v.UserId == userId && (context_ == null || v.Context == context_))
            .OrderBy(v => v.SortOrder).ThenBy(v => v.Name)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<UserSavedView>> ListSharedByContextAsync(string context_, string tenantId, CancellationToken ct)
        => await context.UserSavedViews
            .Where(v => v.IsShared && v.Context == context_ && v.TenantId == tenantId)
            .OrderBy(v => v.SortOrder).ThenBy(v => v.Name)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task AddAsync(UserSavedView view, CancellationToken ct)
    {
        await context.UserSavedViews.AddAsync(view, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(UserSavedView view, CancellationToken ct)
    {
        context.UserSavedViews.Update(view);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(UserSavedView view, CancellationToken ct)
    {
        context.UserSavedViews.Remove(view);
        await context.SaveChangesAsync(ct);
    }
}
