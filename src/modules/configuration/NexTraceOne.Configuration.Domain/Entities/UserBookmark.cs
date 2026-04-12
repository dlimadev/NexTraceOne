using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Domain.Entities;

public sealed record UserBookmarkId(Guid Value) : TypedIdBase(Value);

/// <summary>Favorito de uma entidade da plataforma para um determinado utilizador.</summary>
public sealed class UserBookmark : Entity<UserBookmarkId>
{
    public string UserId { get; private init; } = string.Empty;
    public string TenantId { get; private init; } = string.Empty;
    public BookmarkEntityType EntityType { get; private init; }
    public string EntityId { get; private init; } = string.Empty;
    public string DisplayName { get; private init; } = string.Empty;
    public string? Url { get; private init; }
    public DateTimeOffset CreatedAt { get; private init; }

    private UserBookmark() { }

    public static UserBookmark Create(
        string userId,
        string tenantId,
        BookmarkEntityType entityType,
        string entityId,
        string displayName,
        string? url = null)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        Guard.Against.StringTooLong(userId, 200, nameof(userId));
        Guard.Against.NullOrWhiteSpace(tenantId, nameof(tenantId));
        Guard.Against.StringTooLong(tenantId, 200, nameof(tenantId));
        Guard.Against.NullOrWhiteSpace(entityId, nameof(entityId));
        Guard.Against.StringTooLong(entityId, 256, nameof(entityId));
        Guard.Against.NullOrWhiteSpace(displayName, nameof(displayName));
        Guard.Against.StringTooLong(displayName, 300, nameof(displayName));
        if (url is not null) Guard.Against.StringTooLong(url, 2000, nameof(url));

        return new UserBookmark
        {
            Id = new UserBookmarkId(Guid.NewGuid()),
            UserId = userId.Trim(),
            TenantId = tenantId.Trim(),
            EntityType = entityType,
            EntityId = entityId.Trim(),
            DisplayName = displayName.Trim(),
            Url = url?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
