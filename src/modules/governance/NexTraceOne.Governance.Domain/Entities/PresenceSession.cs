using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Governance.Domain.Entities;

public sealed record PresenceSessionId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Sessão de presença de um utilizador num dashboard ou notebook em tempo real.
/// V3.7 — Real-time Collaboration &amp; War Room.
/// </summary>
public sealed class PresenceSession : Entity<PresenceSessionId>
{
    public string ResourceType { get; private init; } = string.Empty; // "dashboard" | "notebook"
    public Guid ResourceId { get; private init; }
    public string TenantId { get; private init; } = string.Empty;
    public string UserId { get; private init; } = string.Empty;
    public string DisplayName { get; private init; } = string.Empty;
    public string AvatarColor { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset JoinedAt { get; private init; }
    public DateTimeOffset LastSeenAt { get; private set; }
    public DateTimeOffset? LeftAt { get; private set; }

    private PresenceSession() { }

    public static PresenceSession Join(
        string resourceType,
        Guid resourceId,
        string tenantId,
        string userId,
        string displayName,
        string avatarColor,
        DateTimeOffset now)
    {
        return new PresenceSession
        {
            Id = new PresenceSessionId(Guid.NewGuid()),
            ResourceType = resourceType,
            ResourceId = resourceId,
            TenantId = tenantId,
            UserId = userId,
            DisplayName = displayName,
            AvatarColor = avatarColor,
            IsActive = true,
            JoinedAt = now,
            LastSeenAt = now,
        };
    }

    public void Heartbeat(DateTimeOffset now) => LastSeenAt = now;

    public void Leave(DateTimeOffset now)
    {
        IsActive = false;
        LeftAt = now;
    }
}
