using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Application.Abstractions;

/// <summary>Abstração do repositório de favoritos de utilizador.</summary>
public interface IUserBookmarkRepository
{
    Task<UserBookmark?> GetByIdAsync(UserBookmarkId id, CancellationToken ct);
    Task<UserBookmark?> FindAsync(string userId, string tenantId, BookmarkEntityType entityType, string entityId, CancellationToken ct);
    Task<IReadOnlyList<UserBookmark>> ListByUserAsync(string userId, string? tenantId, BookmarkEntityType? entityType, CancellationToken ct);
    Task AddAsync(UserBookmark bookmark, CancellationToken ct);
    Task DeleteAsync(UserBookmark bookmark, CancellationToken ct);
}
