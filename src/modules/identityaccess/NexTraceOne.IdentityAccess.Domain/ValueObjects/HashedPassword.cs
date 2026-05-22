using System.Security.Cryptography;
using System.Text;

using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.IdentityAccess.Domain.ValueObjects;

/// <summary>
/// Value Object que encapsula o hash PBKDF2-SHA256 de uma senha local.
/// v2: 600 000 iterações (OWASP 2024). v1 (100 000 iter.) suportado apenas para leitura.
/// </summary>
public sealed class HashedPassword : ValueObject
{
    // v1 legado — 100k iterações; suportado apenas na verificação, nunca criado.
    private const int V1Iterations = 100_000;
    // v2 atual — 600k iterações (OWASP ASVS 4.0 / 2024 para PBKDF2-SHA256).
    private const int V2Iterations = 600_000;
    private const int MinPasswordLength = 12;

    private HashedPassword(string value)
    {
        Value = value;
    }

    /// <summary>Valor bruto do hash PBKDF2 persistido.</summary>
    public string Value { get; }

    /// <summary>Cria um hash PBKDF2-SHA256 v2 (600k iterações) a partir de uma senha em texto plano.</summary>
    public static HashedPassword FromPlainText(string password)
    {
        var normalized = Guard.Against.NullOrWhiteSpace(password);

        if (normalized.Length < MinPasswordLength)
        {
            throw new ArgumentException(
                $"Password must contain at least {MinPasswordLength} characters.", nameof(password));
        }

        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(normalized),
            salt,
            V2Iterations,
            HashAlgorithmName.SHA256,
            32);

        return new HashedPassword($"v2.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}");
    }

    /// <summary>Reconstrói o value object a partir de um hash já persistido.</summary>
    public static HashedPassword FromHash(string hash)
        => new(Guard.Against.NullOrWhiteSpace(hash));

    /// <summary>
    /// Verifica se a senha em texto plano corresponde ao hash atual.
    /// Suporta v1 (100k iter., legado) e v2 (600k iter., atual).
    /// </summary>
    public bool Verify(string password)
    {
        var normalized = Guard.Against.NullOrWhiteSpace(password);
        var parts = Value.Split('.', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 3 || (parts[0] != "v1" && parts[0] != "v2"))
        {
            return false;
        }

        var iterations = parts[0] == "v2" ? V2Iterations : V1Iterations;
        var salt = Convert.FromBase64String(parts[1]);
        var expectedHash = Convert.FromBase64String(parts[2]);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(normalized),
            salt,
            iterations,
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
