using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>
/// Token de activação de conta enviado por email após criação de utilizador.
/// Apenas o hash SHA-256 do token é persistido — o valor raw é enviado uma única vez.
/// </summary>
public sealed class AccountActivationToken : Entity<AccountActivationTokenId>
{
    public static readonly TimeSpan DefaultExpiry = TimeSpan.FromHours(48);

    private AccountActivationToken() { }

    public UserId UserId { get; private set; } = null!;

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? UsedAt { get; private set; }

    public bool IsUsed => UsedAt.HasValue;

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public bool IsValid(DateTimeOffset now) => !IsUsed && !IsExpired(now);

    public static AccountActivationToken Create(UserId userId, string tokenHash, DateTimeOffset now)
    {
        return new AccountActivationToken
        {
            Id = AccountActivationTokenId.New(),
            UserId = Guard.Against.Null(userId),
            TokenHash = Guard.Against.NullOrWhiteSpace(tokenHash),
            CreatedAt = now,
            ExpiresAt = now.Add(DefaultExpiry)
        };
    }

    public void MarkUsed(DateTimeOffset now) => UsedAt = now;
}

public sealed record AccountActivationTokenId(Guid Value) : TypedIdBase(Value)
{
    public static AccountActivationTokenId New() => new(Guid.NewGuid());
    public static AccountActivationTokenId From(Guid id) => new(id);
}
