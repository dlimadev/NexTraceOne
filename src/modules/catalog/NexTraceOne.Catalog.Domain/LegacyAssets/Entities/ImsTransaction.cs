using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.LegacyAssets.Enums;

namespace NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

/// <summary>
/// Transação IMS — processamento de mensagens ou batch com acesso DL/I.
/// </summary>
public sealed class ImsTransaction : Entity<ImsTransactionId>
{
    private ImsTransaction() { }

    // ── Identidade ────────────────────────────────────────────────────

    /// <summary>Código da transação IMS.</summary>
    public string TransactionCode { get; private set; } = string.Empty;

    /// <summary>Nome de apresentação da transação.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Descrição da transação e a sua finalidade.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Sistema mainframe ao qual a transação pertence.</summary>
    public MainframeSystemId SystemId { get; private set; } = null!;

    // ── Execução ──────────────────────────────────────────────────────

    /// <summary>Tipo de transação IMS (MPP, BMP, FastPath, IFP).</summary>
    public ImsTransactionType TransactionType { get; private set; } = ImsTransactionType.MPP;

    /// <summary>Nome do PSB (Program Specification Block).</summary>
    public string PsbName { get; private set; } = string.Empty;

    /// <summary>Nome do DBD (Database Description).</summary>
    public string DbdName { get; private set; } = string.Empty;

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

    /// <summary>Cria uma nova transação IMS com os campos obrigatórios.</summary>
    public static ImsTransaction Create(
        string transactionCode, MainframeSystemId systemId, string psbName)
    {
        Guard.Against.NullOrWhiteSpace(transactionCode);
        Guard.Against.Null(systemId);
        Guard.Against.NullOrWhiteSpace(psbName);

        return new ImsTransaction
        {
            Id = ImsTransactionId.New(),
            TransactionCode = transactionCode.Trim().ToUpperInvariant(),
            DisplayName = transactionCode.Trim().ToUpperInvariant(),
            SystemId = systemId,
            PsbName = psbName.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    // ── Mutações controladas ──────────────────────────────────────────

    /// <summary>Atualiza detalhes e classificação da transação.</summary>
    public void UpdateDetails(
        string displayName, string description,
        ImsTransactionType transactionType, string dbdName,
        Criticality criticality, LifecycleStatus lifecycleStatus)
    {
        DisplayName = displayName ?? string.Empty;
        Description = description ?? string.Empty;
        TransactionType = transactionType;
        DbdName = dbdName ?? string.Empty;
        Criticality = criticality;
        LifecycleStatus = lifecycleStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>Identificador fortemente tipado de ImsTransaction.</summary>
public sealed record ImsTransactionId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ImsTransactionId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ImsTransactionId From(Guid id) => new(id);
}
