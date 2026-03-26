using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

/// <summary>
/// Definição persistida de um SLA (Service Level Agreement) associado a um SLO.
/// Representa o compromisso externo ou contratual derivado de um objetivo de nível de serviço.
///
/// Um SLA pode incluir consequências formais (penalidades, créditos) em caso de violação.
/// A relação com SloDefinition permite rastrear qual objetivo interno fundamenta o acordo externo.
/// </summary>
public sealed class SlaDefinition : AuditableEntity<SlaDefinitionId>
{
    private SlaDefinition() { }

    /// <summary>Identificador do tenant.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Identificador do SLO que fundamenta este SLA.</summary>
    public SloDefinitionId SloDefinitionId { get; private set; } = null!;

    /// <summary>Nome do SLA (ex: "API Tier-1 — SLA contratual").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descrição do acordo e das partes envolvidas.</summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Objetivo contratual em percentagem (pode ser igual ou inferior ao SLO).
    /// Exemplo: SLO = 99.9%, SLA = 99.5%.
    /// </summary>
    public decimal ContractualTargetPercent { get; private set; }

    /// <summary>Estado atual do SLA face ao cumprimento do objetivo.</summary>
    public SlaStatus Status { get; private set; }

    /// <summary>Data de início da vigência do SLA.</summary>
    public DateTimeOffset EffectiveFrom { get; private set; }

    /// <summary>Data de fim da vigência do SLA (nulo = sem expiração definida).</summary>
    public DateTimeOffset? EffectiveTo { get; private set; }

    /// <summary>Indica se existem penalidades ou créditos definidos para violação.</summary>
    public bool HasPenaltyClauses { get; private set; }

    /// <summary>Notas adicionais sobre penalidades ou compensações (texto livre).</summary>
    public string? PenaltyNotes { get; private set; }

    /// <summary>Indica se o SLA está ativo.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>Navegação para o SLO base.</summary>
    public SloDefinition SloDefinition { get; private set; } = null!;

    /// <summary>
    /// Cria uma nova definição de SLA associada a um SLO existente.
    /// </summary>
    public static SlaDefinition Create(
        Guid tenantId,
        SloDefinitionId sloDefinitionId,
        string name,
        decimal contractualTargetPercent,
        DateTimeOffset effectiveFrom,
        string? description = null,
        DateTimeOffset? effectiveTo = null,
        bool hasPenaltyClauses = false,
        string? penaltyNotes = null)
    {
        Guard.Against.Default(tenantId);
        Guard.Against.Null(sloDefinitionId);
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.OutOfRange(contractualTargetPercent, nameof(contractualTargetPercent), 0m, 100m);

        return new SlaDefinition
        {
            Id = SlaDefinitionId.New(),
            TenantId = tenantId,
            SloDefinitionId = sloDefinitionId,
            Name = name,
            ContractualTargetPercent = contractualTargetPercent,
            Description = description,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            HasPenaltyClauses = hasPenaltyClauses,
            PenaltyNotes = penaltyNotes,
            Status = SlaStatus.Active,
            IsActive = true
        };
    }

    /// <summary>Marca o SLA como violado.</summary>
    public void MarkBreached() => Status = SlaStatus.Breached;

    /// <summary>Marca o SLA como em risco.</summary>
    public void MarkAtRisk() => Status = SlaStatus.AtRisk;

    /// <summary>Repõe o estado do SLA para ativo/conforme.</summary>
    public void MarkActive() => Status = SlaStatus.Active;

    /// <summary>Desativa o SLA.</summary>
    public void Deactivate() => IsActive = false;
}

/// <summary>Identificador fortemente tipado de SlaDefinition.</summary>
public sealed record SlaDefinitionId(Guid Value) : TypedIdBase(Value)
{
    public static SlaDefinitionId New() => new(Guid.NewGuid());
    public static SlaDefinitionId From(Guid id) => new(id);
}
