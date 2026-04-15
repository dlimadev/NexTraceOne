using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Domain.Graph.Entities;

/// <summary>
/// Representa o vínculo entre uma interface de serviço e uma versão de contrato.
/// Permite rastrear qual versão de contrato está activa para uma dada interface
/// e em qual ambiente, com trilha completa de activação e desactivação.
/// </summary>
public sealed class ContractBinding : AuditableEntity<ContractBindingId>
{
    private ContractBinding() { }

    // ── Referências ───────────────────────────────────────────────────

    /// <summary>Identificador da interface de serviço vinculada.</summary>
    public Guid ServiceInterfaceId { get; private set; }

    /// <summary>Identificador da versão do contrato vinculada.</summary>
    public Guid ContractVersionId { get; private set; }

    // ── Estado ────────────────────────────────────────────────────────

    /// <summary>Estado actual do vínculo.</summary>
    public ContractBindingStatus Status { get; private set; } = ContractBindingStatus.Active;

    /// <summary>Ambiente onde este vínculo está em vigor (prod, staging, etc.).</summary>
    public string BindingEnvironment { get; private set; } = string.Empty;

    /// <summary>Indica se esta é a versão padrão/canónica para a interface neste ambiente.</summary>
    public bool IsDefaultVersion { get; private set; }

    // ── Activação ─────────────────────────────────────────────────────

    /// <summary>Timestamp de activação do vínculo.</summary>
    public DateTimeOffset? ActivatedAt { get; private set; }

    /// <summary>Utilizador que activou o vínculo.</summary>
    public string? ActivatedBy { get; private set; }

    // ── Desactivação ──────────────────────────────────────────────────

    /// <summary>Timestamp de desactivação do vínculo.</summary>
    public DateTimeOffset? DeactivatedAt { get; private set; }

    /// <summary>Utilizador que desactivou o vínculo.</summary>
    public string? DeactivatedBy { get; private set; }

    // ── Notas ─────────────────────────────────────────────────────────

    /// <summary>Notas sobre a migração ou contexto de transição de versão.</summary>
    public string MigrationNotes { get; private set; } = string.Empty;

    // ── Concorrência ──────────────────────────────────────────────────

    /// <summary>
    /// Token de concorrência otimista (PostgreSQL xmin).
    /// Utilizado pelo EF Core para detetar conflitos de escrita concorrente.
    /// </summary>
    public uint RowVersion { get; set; }

    // ── Factory ───────────────────────────────────────────────────────

    /// <summary>Cria um novo vínculo de contrato para uma interface.</summary>
    public static ContractBinding Create(
        Guid serviceInterfaceId,
        Guid contractVersionId,
        string bindingEnvironment)
        => new()
        {
            Id = ContractBindingId.New(),
            ServiceInterfaceId = serviceInterfaceId,
            ContractVersionId = contractVersionId,
            BindingEnvironment = bindingEnvironment ?? string.Empty,
            Status = ContractBindingStatus.Active
        };

    // ── Mutações controladas ──────────────────────────────────────────

    /// <summary>Define se este vínculo representa a versão padrão da interface.</summary>
    public void SetAsDefault(bool isDefault)
    {
        IsDefaultVersion = isDefault;
    }

    /// <summary>Desactiva o vínculo, registando quem e quando o fez.</summary>
    public void Deactivate(string deactivatedBy, DateTimeOffset at)
    {
        Guard.Against.NullOrWhiteSpace(deactivatedBy);
        Status = ContractBindingStatus.Deprecated;
        DeactivatedAt = at;
        DeactivatedBy = deactivatedBy;
    }
}

/// <summary>Identificador fortemente tipado de ContractBinding.</summary>
public sealed record ContractBindingId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ContractBindingId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ContractBindingId From(Guid id) => new(id);
}
