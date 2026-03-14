using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.CommercialCatalog.Domain.Entities;

/// <summary>
/// Entidade de associação N:N entre Plan e FeaturePack.
/// Permite que um plano inclua múltiplos pacotes de funcionalidades
/// e que um pacote seja reutilizado por diferentes planos.
///
/// Decisão de design:
/// - Entidade independente (não aggregate root) para evitar acoplamento
///   direto entre os aggregates Plan e FeaturePack.
/// - Não contém lógica de negócio além da associação.
/// - Unicidade do par (PlanId, FeaturePackId) deve ser garantida pelo repositório.
/// </summary>
public sealed class PlanFeaturePackMapping : Entity<PlanFeaturePackMappingId>
{
    private PlanFeaturePackMapping() { }

    /// <summary>Identificador do plano associado.</summary>
    public PlanId PlanId { get; private set; } = null!;

    /// <summary>Identificador do pacote de funcionalidades associado.</summary>
    public FeaturePackId FeaturePackId { get; private set; } = null!;

    /// <summary>
    /// Factory method para criação de uma associação entre plano e pacote.
    /// </summary>
    /// <param name="planId">Identificador do plano.</param>
    /// <param name="featurePackId">Identificador do pacote de funcionalidades.</param>
    public static PlanFeaturePackMapping Create(PlanId planId, FeaturePackId featurePackId)
    {
        Guard.Against.Null(planId);
        Guard.Against.Null(featurePackId);

        return new PlanFeaturePackMapping
        {
            Id = PlanFeaturePackMappingId.New(),
            PlanId = planId,
            FeaturePackId = featurePackId
        };
    }
}

/// <summary>Identificador fortemente tipado de PlanFeaturePackMapping.</summary>
public sealed record PlanFeaturePackMappingId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static PlanFeaturePackMappingId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static PlanFeaturePackMappingId From(Guid id) => new(id);
}
