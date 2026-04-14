using Microsoft.EntityFrameworkCore;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Repositories;

internal sealed class UserBookmarkRepository(ConfigurationDbContext context) : IUserBookmarkRepository
{
    public async Task<UserBookmark?> GetByIdAsync(UserBookmarkId id, CancellationToken ct)
        => await context.UserBookmarks.SingleOrDefaultAsync(b => b.Id == id, ct);

    public async Task<UserBookmark?> FindAsync(string userId, string tenantId, BookmarkEntityType entityType, string entityId, CancellationToken ct)
        => await context.UserBookmarks
            .SingleOrDefaultAsync(b => b.UserId == userId && b.TenantId == tenantId
                && b.EntityType == entityType && b.EntityId == entityId, ct);

    public async Task<IReadOnlyList<UserBookmark>> ListByUserAsync(string userId, string? tenantId, BookmarkEntityType? entityType, CancellationToken ct)
        => await context.UserBookmarks
            .Where(b => b.UserId == userId
                && (tenantId == null || b.TenantId == tenantId)
                && (entityType == null || b.EntityType == entityType))
            .OrderByDescending(b => b.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task AddAsync(UserBookmark bookmark, CancellationToken ct)
    {
        await context.UserBookmarks.AddAsync(bookmark, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(UserBookmark bookmark, CancellationToken ct)
    {
        context.UserBookmarks.Remove(bookmark);
        await context.SaveChangesAsync(ct);
    }
}
