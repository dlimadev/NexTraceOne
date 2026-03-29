using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

/// <summary>
/// Binding z/OS Connect — exposição de transações mainframe via REST API.
/// Mapeamento entre operação REST e transação CICS/IMS.
/// </summary>
public sealed class ZosConnectBinding : Entity<ZosConnectBindingId>
{
    private ZosConnectBinding() { }

    // ── Identidade ────────────────────────────────────────────────────

    /// <summary>Nome do binding (identificador técnico).</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Nome de apresentação do binding.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Descrição do binding e a sua finalidade.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Sistema mainframe ao qual o binding pertence.</summary>
    public MainframeSystemId SystemId { get; private set; } = null!;

    // ── Mapeamento REST ↔ Mainframe ───────────────────────────────────

    /// <summary>Nome do serviço z/OS Connect.</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Nome da operação REST exposta.</summary>
    public string OperationName { get; private set; } = string.Empty;

    /// <summary>Método HTTP da operação (GET, POST, PUT, etc.).</summary>
    public string HttpMethod { get; private set; } = string.Empty;

    /// <summary>Base path da operação REST.</summary>
    public string BasePath { get; private set; } = string.Empty;

    /// <summary>Transação CICS/IMS alvo do binding.</summary>
    public string TargetTransaction { get; private set; } = string.Empty;

    /// <summary>Schema JSON do request.</summary>
    public string RequestSchema { get; private set; } = string.Empty;

    /// <summary>Schema JSON do response.</summary>
    public string ResponseSchema { get; private set; } = string.Empty;

    // ── Classificação ─────────────────────────────────────────────────

    /// <summary>Nível de criticidade do binding para o negócio.</summary>
    public Criticality Criticality { get; private set; } = Criticality.Medium;

    /// <summary>Estado do ciclo de vida do binding.</summary>
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

    /// <summary>Cria um novo binding z/OS Connect com os campos obrigatórios.</summary>
    public static ZosConnectBinding Create(
        string name, MainframeSystemId systemId,
        string serviceName, string operationName)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.Null(systemId);
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(operationName);

        return new ZosConnectBinding
        {
            Id = ZosConnectBindingId.New(),
            Name = name.Trim(),
            DisplayName = name.Trim(),
            SystemId = systemId,
            ServiceName = serviceName.Trim(),
            OperationName = operationName.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    // ── Mutações controladas ──────────────────────────────────────────

    /// <summary>Atualiza detalhes e classificação do binding.</summary>
    public void UpdateDetails(
        string displayName, string description,
        string httpMethod, string basePath,
        string targetTransaction, string requestSchema, string responseSchema,
        Criticality criticality, LifecycleStatus lifecycleStatus)
    {
        DisplayName = displayName ?? string.Empty;
        Description = description ?? string.Empty;
        HttpMethod = httpMethod ?? string.Empty;
        BasePath = basePath ?? string.Empty;
        TargetTransaction = targetTransaction ?? string.Empty;
        RequestSchema = requestSchema ?? string.Empty;
        ResponseSchema = responseSchema ?? string.Empty;
        Criticality = criticality;
        LifecycleStatus = lifecycleStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>Identificador fortemente tipado de ZosConnectBinding.</summary>
public sealed record ZosConnectBindingId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ZosConnectBindingId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ZosConnectBindingId From(Guid id) => new(id);
}
