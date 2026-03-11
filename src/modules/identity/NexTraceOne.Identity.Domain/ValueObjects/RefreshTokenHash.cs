using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Domain.Primitives;
using System.Security.Cryptography;
using System.Text;

namespace NexTraceOne.Identity.Domain.ValueObjects;

/// <summary>
/// Value Object que encapsula o hash SHA-256 de um refresh token.
/// </summary>
public sealed class RefreshTokenHash : ValueObject
{
    private RefreshTokenHash(string value)
    {
        Value = value;
    }

    /// <summary>Hash hexadecimal persistido do refresh token.</summary>
    public string Value { get; }

    /// <summary>Calcula o hash SHA-256 de um refresh token em texto plano.</summary>
    public static RefreshTokenHash Create(string refreshToken)
    {
        var normalized = Guard.Against.NullOrWhiteSpace(refreshToken);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return new RefreshTokenHash(Convert.ToHexString(bytes));
    }

    /// <summary>Reconstrói o hash já persistido no banco.</summary>
    public static RefreshTokenHash FromHash(string hash)
        => new(Guard.Against.NullOrWhiteSpace(hash));

    /// <summary>Compara o token em texto plano com o hash atual.</summary>
    public bool Matches(string refreshToken)
        => string.Equals(Create(refreshToken).Value, Value, StringComparison.Ordinal);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <summary>Retorna o hash persistido.</summary>
    public override string ToString() => Value;
}
