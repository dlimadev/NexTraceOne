using System.Security.Cryptography;
using System.Text;
using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.Audit.Domain.Entities;

/// <summary>
/// Entidade que representa um elo da cadeia de hash de auditoria.
/// Cada link contém o hash do evento atual combinado com o hash do link anterior,
/// formando uma cadeia iminável tipo blockchain.
/// </summary>
public sealed class AuditChainLink : Entity<AuditChainLinkId>
{
    private AuditChainLink() { }

    /// <summary>Número sequencial do link na cadeia.</summary>
    public long SequenceNumber { get; private set; }

    /// <summary>Hash SHA-256 do conteúdo deste link.</summary>
    public string CurrentHash { get; private set; } = string.Empty;

    /// <summary>Hash SHA-256 do link anterior na cadeia.</summary>
    public string PreviousHash { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC da criação do link.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Cria um novo link da cadeia de hash para um evento de auditoria.</summary>
    public static AuditChainLink Create(
        AuditEvent auditEvent,
        long sequenceNumber,
        string previousHash,
        DateTimeOffset createdAt)
    {
        Guard.Against.Null(auditEvent);
        Guard.Against.Negative(sequenceNumber);

        var currentHash = ComputeHash(auditEvent, sequenceNumber, previousHash);

        return new AuditChainLink
        {
            Id = AuditChainLinkId.New(),
            SequenceNumber = sequenceNumber,
            CurrentHash = currentHash,
            PreviousHash = previousHash ?? string.Empty,
            CreatedAt = createdAt
        };
    }

    /// <summary>Verifica se o hash atual é válido para o evento e hash anterior informados.</summary>
    public bool Verify(AuditEvent auditEvent, string previousHash)
    {
        var expectedHash = ComputeHash(auditEvent, SequenceNumber, previousHash);
        return string.Equals(CurrentHash, expectedHash, StringComparison.Ordinal);
    }

    private static string ComputeHash(AuditEvent evt, long sequence, string previousHash)
    {
        var payload = $"{sequence}|{evt.Id.Value}|{evt.SourceModule}|{evt.ActionType}|{evt.ResourceId}|{evt.PerformedBy}|{evt.OccurredAt:O}|{previousHash}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash);
    }
}

/// <summary>Identificador fortemente tipado de AuditChainLink.</summary>
public sealed record AuditChainLinkId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AuditChainLinkId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AuditChainLinkId From(Guid id) => new(id);
}
