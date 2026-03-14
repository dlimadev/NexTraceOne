using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.RulesetGovernance.Domain.Entities;

/// <summary>
/// Entidade que associa um Ruleset a um tipo de ativo (asset type).
/// Permite definir quais rulesets são aplicados automaticamente para cada tipo de API/serviço.
/// </summary>
public sealed class RulesetBinding : AuditableEntity<RulesetBindingId>
{
    private RulesetBinding() { }

    /// <summary>Identificador do ruleset vinculado.</summary>
    public RulesetId RulesetId { get; private set; } = null!;

    /// <summary>Tipo do ativo ao qual o ruleset está vinculado (ex: "REST", "gRPC", "GraphQL").</summary>
    public string AssetType { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC em que o binding foi criado.</summary>
    public DateTimeOffset BindingCreatedAt { get; private set; }

    /// <summary>
    /// Cria um novo vínculo entre ruleset e tipo de ativo.
    /// </summary>
    public static RulesetBinding Create(
        RulesetId rulesetId,
        string assetType,
        DateTimeOffset createdAt)
    {
        Guard.Against.Null(rulesetId);
        Guard.Against.NullOrWhiteSpace(assetType);

        return new RulesetBinding
        {
            Id = RulesetBindingId.New(),
            RulesetId = rulesetId,
            AssetType = assetType,
            BindingCreatedAt = createdAt
        };
    }
}

/// <summary>Identificador fortemente tipado de RulesetBinding.</summary>
public sealed record RulesetBindingId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static RulesetBindingId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static RulesetBindingId From(Guid id) => new(id);
}
