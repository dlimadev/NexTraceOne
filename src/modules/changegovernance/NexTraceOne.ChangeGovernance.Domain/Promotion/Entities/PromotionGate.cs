using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;

/// <summary>
/// Entidade que representa um gate (portão de verificação) vinculado a um ambiente de deployment.
/// Cada gate define um critério que deve ser satisfeito antes de permitir a promoção.
/// Exemplos: MinApprovals, AllTestsPassed, ScanPassed, EvidencePackComplete.
/// </summary>
public sealed class PromotionGate : AuditableEntity<PromotionGateId>
{
    private PromotionGate() { }

    /// <summary>Identificador do ambiente de deployment ao qual este gate pertence.</summary>
    public DeploymentEnvironmentId DeploymentEnvironmentId { get; private set; } = default!;

    /// <summary>Nome do gate (ex: MinApprovals, AllTestsPassed, ScanPassed).</summary>
    public string GateName { get; private set; } = string.Empty;

    /// <summary>Tipo/categoria do gate para agrupamento e filtragem.</summary>
    public string GateType { get; private set; } = string.Empty;

    /// <summary>Indica se o gate é obrigatório para que a promoção seja aprovada.</summary>
    public bool IsRequired { get; private set; }

    /// <summary>Indica se o gate está ativo e será avaliado nas promoções.</summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Cria um novo gate de promoção vinculado a um ambiente de deployment.
    /// </summary>
    public static PromotionGate Create(
        DeploymentEnvironmentId environmentId,
        string gateName,
        string gateType,
        bool isRequired)
    {
        Guard.Against.Null(environmentId);
        Guard.Against.NullOrWhiteSpace(gateName);
        Guard.Against.StringTooLong(gateName, 200);
        Guard.Against.Null(gateType);
        Guard.Against.StringTooLong(gateType, 100);

        return new PromotionGate
        {
            Id = PromotionGateId.New(),
            DeploymentEnvironmentId = environmentId,
            GateName = gateName,
            GateType = gateType,
            IsRequired = isRequired,
            IsActive = true
        };
    }

    /// <summary>Ativa o gate para ser avaliado nas próximas promoções.</summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>Desativa o gate, removendo-o da avaliação de promoções futuras.</summary>
    public void Deactivate()
    {
        IsActive = false;
    }
}

/// <summary>Identificador fortemente tipado de PromotionGate.</summary>
public sealed record PromotionGateId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static PromotionGateId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static PromotionGateId From(Guid id) => new(id);
}
