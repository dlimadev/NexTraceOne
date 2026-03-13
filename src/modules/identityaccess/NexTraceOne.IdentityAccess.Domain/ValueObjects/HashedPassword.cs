using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Domain.Primitives;
using System.Security.Cryptography;
using System.Text;

namespace NexTraceOne.Identity.Domain.ValueObjects;

/// <summary>
/// Value Object que encapsula o hash BCrypt de uma senha local.
/// </summary>
public sealed class HashedPassword : ValueObject
{
    private HashedPassword(string value)
    {
        Value = value;
    }

    /// <summary>Valor bruto do hash BCrypt persistido.</summary>
    public string Value { get; }

    /// <summary>Cria um hash BCrypt a partir de uma senha em texto plano.</summary>
    public static HashedPassword FromPlainText(string password)
    {
        var normalized = Guard.Against.NullOrWhiteSpace(password);

        if (normalized.Length < 8)
        {
            throw new ArgumentException("Password must contain at least 8 characters.", nameof(password));
        }

        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(normalized),
            salt,
            100_000,
            HashAlgorithmName.SHA256,
            32);

        return new HashedPassword($"v1.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}");
    }

    /// <summary>Reconstrói o value object a partir de um hash já persistido.</summary>
    public static HashedPassword FromHash(string hash)
        => new(Guard.Against.NullOrWhiteSpace(hash));

    /// <summary>Verifica se a senha em texto plano corresponde ao hash atual.</summary>
    public bool Verify(string password)
    {
        var normalized = Guard.Against.NullOrWhiteSpace(password);
        var parts = Value.Split('.', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 3 || parts[0] != "v1")
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[1]);
        var expectedHash = Convert.FromBase64String(parts[2]);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(normalized),
            salt,
            100_000,
            HashAlgorithmName.SHA256,
            32);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <summary>Retorna o hash persistido.</summary>
    public override string ToString() => Value;
}
