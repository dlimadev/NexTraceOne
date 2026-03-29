using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.LegacyAssets.Enums;
using NexTraceOne.Catalog.Domain.LegacyAssets.ValueObjects;

namespace NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

/// <summary>
/// Transação CICS — processamento online no mainframe.
/// Associada a um programa COBOL e a uma região CICS.
/// </summary>
public sealed class CicsTransaction : Entity<CicsTransactionId>
{
    private CicsTransaction() { }

    // ── Identidade ────────────────────────────────────────────────────

    /// <summary>Código da transação CICS (máx. 4 caracteres).</summary>
    public string TransactionId { get; private set; } = string.Empty;

    /// <summary>Nome de apresentação da transação.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Descrição da transação e a sua finalidade.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Sistema mainframe ao qual a transação pertence.</summary>
    public MainframeSystemId SystemId { get; private set; } = null!;

    // ── Execução ──────────────────────────────────────────────────────

    /// <summary>Nome do programa COBOL associado.</summary>
    public string ProgramName { get; private set; } = string.Empty;

    /// <summary>Tipo de transação CICS.</summary>
    public CicsTransactionType TransactionType { get; private set; } = CicsTransactionType.Online;

    /// <summary>Região CICS onde a transação é executada.</summary>
    public CicsRegion Region { get; private set; } = null!;

    /// <summary>Tamanho da COMMAREA em bytes (quando aplicável).</summary>
    public int? CommareaLength { get; private set; }

    // ── Classificação ─────────────────────────────────────────────────

    /// <summary>Nível de criticidade da transação para o negócio.</summary>
    public Criticality Criticality { get; private set; } = Criticality.Medium;

    /// <summary>Estado do ciclo de vida da transação.</summary>
    public LifecycleStatus LifecycleStatus { get; private set; } = LifecycleStatus.Active;

    // ── Auditoria ─────────────────────────────────────────────────────

    /// <summary>Data de criação do registo.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data da última atualização.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    // ── Concorrência ──────────────────────────────────────────────────

    /// <summary>
    /// Token de concorrência otimista (PostgreSQL xmin).
    /// </summary>
    public uint RowVersion { get; set; }

    // ── Factory method ────────────────────────────────────────────────

    /// <summary>Cria uma nova transação CICS com os campos obrigatórios.</summary>
    public static CicsTransaction Create(
        string transactionId, MainframeSystemId systemId,
        string programName, CicsRegion region)
    {
        Guard.Against.NullOrWhiteSpace(transactionId);
        Guard.Against.StringTooLong(transactionId, 4, nameof(transactionId));
        Guard.Against.Null(systemId);
        Guard.Against.NullOrWhiteSpace(programName);
        Guard.Against.Null(region);

        return new CicsTransaction
        {
            Id = CicsTransactionId.New(),
            TransactionId = transactionId.Trim().ToUpperInvariant(),
            DisplayName = transactionId.Trim().ToUpperInvariant(),
            SystemId = systemId,
            ProgramName = programName.Trim(),
            Region = region,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    // ── Mutações controladas ──────────────────────────────────────────

    /// <summary>Atualiza detalhes e classificação da transação.</summary>
    public void UpdateDetails(
        string displayName, string description,
        CicsTransactionType transactionType, int? commareaLength,
        Criticality criticality, LifecycleStatus lifecycleStatus)
    {
        DisplayName = displayName ?? string.Empty;
        Description = description ?? string.Empty;
        TransactionType = transactionType;
        CommareaLength = commareaLength;
        Criticality = criticality;
        LifecycleStatus = lifecycleStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>Identificador fortemente tipado de CicsTransaction.</summary>
public sealed record CicsTransactionId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static CicsTransactionId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static CicsTransactionId From(Guid id) => new(id);
}
