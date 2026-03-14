using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.CommercialCatalog.Domain.Entities;

/// <summary>
/// Entidade filha de FeaturePack que representa uma capability individual
/// dentro de um pacote de funcionalidades.
///
/// Decisão de design:
/// - Entity (não AggregateRoot) porque só existe no contexto de um FeaturePack.
/// - CapabilityCode referencia o mesmo vocabulário de capabilities do Licensing
///   (ex: "catalog:read", "contracts:diff") mas sem acoplamento direto.
/// - DefaultLimit permite que o pacote sugira um limite padrão de uso
///   para cada capability, que pode ser sobrescrito ao associar a uma licença.
/// </summary>
public sealed class FeaturePackItem : Entity<FeaturePackItemId>
{
    private FeaturePackItem() { }

    /// <summary>Identificador do FeaturePack ao qual este item pertence.</summary>
    public FeaturePackId FeaturePackId { get; private set; } = null!;

    /// <summary>Código da capability (ex: "catalog:write"). Mesmo vocabulário do Licensing.</summary>
    public string CapabilityCode { get; private set; } = string.Empty;

    /// <summary>Nome amigável da capability para exibição.</summary>
    public string CapabilityName { get; private set; } = string.Empty;

    /// <summary>Limite padrão de uso sugerido pelo pacote (null = ilimitado).</summary>
    public int? DefaultLimit { get; private set; }

    /// <summary>
    /// Factory method para criação de um item de pacote de funcionalidades.
    /// </summary>
    /// <param name="featurePackId">Identificador do FeaturePack pai.</param>
    /// <param name="capabilityCode">Código da capability.</param>
    /// <param name="capabilityName">Nome amigável da capability.</param>
    /// <param name="defaultLimit">Limite padrão de uso (null = ilimitado).</param>
    public static FeaturePackItem Create(
        FeaturePackId featurePackId,
        string capabilityCode,
        string capabilityName,
        int? defaultLimit = null)
    {
        Guard.Against.Null(featurePackId);
        Guard.Against.NullOrWhiteSpace(capabilityCode);
        Guard.Against.NullOrWhiteSpace(capabilityName);

        if (defaultLimit.HasValue)
        {
            Guard.Against.NegativeOrZero(defaultLimit.Value);
        }

        return new FeaturePackItem
        {
            Id = FeaturePackItemId.New(),
            FeaturePackId = featurePackId,
            CapabilityCode = capabilityCode,
            CapabilityName = capabilityName,
            DefaultLimit = defaultLimit
        };
    }
}

/// <summary>Identificador fortemente tipado de FeaturePackItem.</summary>
public sealed record FeaturePackItemId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static FeaturePackItemId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static FeaturePackItemId From(Guid id) => new(id);
}
