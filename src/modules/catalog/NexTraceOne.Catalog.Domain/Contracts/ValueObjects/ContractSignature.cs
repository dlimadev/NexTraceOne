using System.Security.Cryptography;
using System.Text;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.Contracts.Domain.ValueObjects;

/// <summary>
/// Value Object que encapsula a assinatura digital de um contrato.
/// Armazena o hash SHA-256 da representação canônica do artefato,
/// garantindo integridade verificável após promoção para produção.
/// A canonicalização elimina diferenças irrelevantes de serialização
/// (espaços, ordem de chaves, newlines) antes do cálculo do hash.
/// </summary>
public sealed class ContractSignature : ValueObject
{
    private ContractSignature() { }

    /// <summary>Hash SHA-256 em hexadecimal da representação canônica.</summary>
    public string Fingerprint { get; private set; } = string.Empty;

    /// <summary>Algoritmo utilizado para o hash (ex: "SHA-256").</summary>
    public string Algorithm { get; private set; } = string.Empty;

    /// <summary>Usuário que realizou a assinatura.</summary>
    public string SignedBy { get; private set; } = string.Empty;

    /// <summary>Timestamp UTC da assinatura.</summary>
    public DateTimeOffset SignedAt { get; private set; }

    /// <summary>
    /// Cria uma nova assinatura a partir do conteúdo canonicalizado do contrato.
    /// Calcula o hash SHA-256 e registra metadados de proveniência.
    /// </summary>
    /// <param name="canonicalContent">Conteúdo canonicalizado do contrato (sem diferenças cosméticas).</param>
    /// <param name="signedBy">Identificador do usuário assinante.</param>
    /// <param name="signedAt">Timestamp UTC da assinatura.</param>
    public static ContractSignature Create(string canonicalContent, string signedBy, DateTimeOffset signedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalContent);
        ArgumentException.ThrowIfNullOrWhiteSpace(signedBy);

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalContent));
        var fingerprint = Convert.ToHexStringLower(hashBytes);

        return new ContractSignature
        {
            Fingerprint = fingerprint,
            Algorithm = "SHA-256",
            SignedBy = signedBy,
            SignedAt = signedAt
        };
    }

    /// <summary>
    /// Verifica se o fingerprint armazenado corresponde ao conteúdo informado.
    /// Utiliza comparação em tempo constante (CryptographicOperations.FixedTimeEquals)
    /// para prevenir ataques de timing side-channel na verificação de integridade.
    /// </summary>
    public bool Verify(string canonicalContent)
    {
        if (string.IsNullOrWhiteSpace(canonicalContent))
            return false;

        var computedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalContent));
        var storedBytes = Convert.FromHexString(Fingerprint);
        return CryptographicOperations.FixedTimeEquals(storedBytes, computedBytes);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Fingerprint;
        yield return Algorithm;
        yield return SignedBy;
        yield return SignedAt;
    }
}
