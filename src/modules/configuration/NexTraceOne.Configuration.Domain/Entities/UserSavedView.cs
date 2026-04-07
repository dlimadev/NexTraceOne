using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Configuration.Domain.Entities;

public sealed record UserSavedViewId(Guid Value) : TypedIdBase(Value);

/// <summary>Vista guardada pelo utilizador para um determinado contexto de lista ou painel.</summary>
public sealed class UserSavedView : Entity<UserSavedViewId>
{
    public string UserId { get; private init; } = string.Empty;
    public string TenantId { get; private init; } = string.Empty;
    public string Context { get; private init; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string FiltersJson { get; private set; } = string.Empty;
    public bool IsShared { get; private set; }
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private UserSavedView() { }

    public static UserSavedView Create(
        string userId,
        string tenantId,
        string context,
        string name,
        string filtersJson,
        string? description = null,
        bool isShared = false,
        int sortOrder = 0)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        Guard.Against.StringTooLong(userId, 200, nameof(userId));
        Guard.Against.NullOrWhiteSpace(tenantId, nameof(tenantId));
        Guard.Against.StringTooLong(tenantId, 200, nameof(tenantId));
        Guard.Against.NullOrWhiteSpace(context, nameof(context));
        Guard.Against.StringTooLong(context, 100, nameof(context));
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.StringTooLong(name, 200, nameof(name));
        Guard.Against.NullOrWhiteSpace(filtersJson, nameof(filtersJson));
        Guard.Against.StringTooLong(filtersJson, 8000, nameof(filtersJson));
        if (description is not null) Guard.Against.StringTooLong(description, 1000, nameof(description));

        return new UserSavedView
        {
            Id = new UserSavedViewId(Guid.NewGuid()),
            UserId = userId.Trim(),
            TenantId = tenantId.Trim(),
            Context = context.Trim(),
            Name = name.Trim(),
            Description = description?.Trim(),
            FiltersJson = filtersJson.Trim(),
            IsShared = isShared,
            SortOrder = sortOrder,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(string name, string? description, string filtersJson, bool isShared)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.StringTooLong(name, 200, nameof(name));
        Guard.Against.NullOrWhiteSpace(filtersJson, nameof(filtersJson));
        Guard.Against.StringTooLong(filtersJson, 8000, nameof(filtersJson));
        if (description is not null) Guard.Against.StringTooLong(description, 1000, nameof(description));

        Name = name.Trim();
        Description = description?.Trim();
        FiltersJson = filtersJson.Trim();
        IsShared = isShared;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
