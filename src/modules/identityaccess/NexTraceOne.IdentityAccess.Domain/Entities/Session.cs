using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>
/// Aggregate Root que representa uma sessão autenticada com refresh token rotacionável.
/// </summary>
public sealed class Session : AggregateRoot<SessionId>
{
    private Session() { }

    /// <summary>Usuário dono da sessão.</summary>
    public UserId UserId { get; private set; } = null!;

    /// <summary>Hash SHA-256 do refresh token persistido.</summary>
    public RefreshTokenHash RefreshToken { get; private set; } = null!;

    /// <summary>Data/hora UTC de expiração da sessão.</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>IP de origem da sessão.</summary>
    public string CreatedByIp { get; private set; } = string.Empty;

    /// <summary>User agent da sessão.</summary>
    public string UserAgent { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC de revogação, quando existir.</summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>Cria uma nova sessão autenticada.</summary>
    public static Session Create(
        UserId userId,
        RefreshTokenHash refreshToken,
        DateTimeOffset expiresAt,
        string createdByIp,
        string userAgent)
        => new()
        {
            Id = SessionId.New(),
            UserId = Guard.Against.Null(userId),
            RefreshToken = Guard.Against.Null(refreshToken),
            ExpiresAt = expiresAt,
            CreatedByIp = Guard.Against.NullOrWhiteSpace(createdByIp),
            UserAgent = Guard.Against.NullOrWhiteSpace(userAgent)
        };

    /// <summary>Revoga a sessão na data informada.</summary>
    public void Revoke(DateTimeOffset revokedAt)
        => RevokedAt ??= revokedAt;

    /// <summary>Rotaciona o refresh token e estende a validade da sessão.</summary>
    public void Rotate(RefreshTokenHash refreshToken, DateTimeOffset expiresAt)
    {
        RefreshToken = Guard.Against.Null(refreshToken);
        ExpiresAt = expiresAt;
        RevokedAt = null;
    }

    /// <summary>Indica se a sessão expirou na data informada.</summary>
    public bool IsExpired(DateTimeOffset now) => ExpiresAt <= now;

    /// <summary>Indica se a sessão está ativa na data informada.</summary>
    public bool IsActive(DateTimeOffset now) => !IsExpired(now) && RevokedAt is null;
}

/// <summary>Identificador fortemente tipado de Session.</summary>
public sealed record SessionId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static SessionId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static SessionId From(Guid id) => new(id);
}
