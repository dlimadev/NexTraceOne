using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Política de roteamento de modelo por intenção de prompt.
/// Define qual modelo preferido e fallback usar para cada intenção classificada,
/// com limites de tokens e custo máximo por pedido.
///
/// Invariantes:
/// - TenantId, Intent e PreferredModelName são obrigatórios.
/// - MaxTokens deve ser positivo.
/// - MaxCostPerRequestUsd deve ser não negativo.
/// </summary>
public sealed class ModelRoutingPolicy : AuditableEntity<ModelRoutingPolicyId>
{
    private ModelRoutingPolicy() { }

    /// <summary>Identificador do tenant dono da política.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Intenção de prompt à qual esta política se aplica.</summary>
    public PromptIntent Intent { get; private set; }

    /// <summary>Nome do modelo preferido para esta intenção.</summary>
    public string PreferredModelName { get; private set; } = string.Empty;

    /// <summary>Nome do modelo de fallback (opcional).</summary>
    public string? FallbackModelName { get; private set; }

    /// <summary>Máximo de tokens permitidos por pedido.</summary>
    public int MaxTokens { get; private set; }

    /// <summary>Custo máximo por pedido em USD.</summary>
    public decimal MaxCostPerRequestUsd { get; private set; }

    /// <summary>Indica se esta política está activa.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Cria uma nova política de roteamento por intenção.</summary>
    public static ModelRoutingPolicy Create(
        Guid tenantId,
        PromptIntent intent,
        string preferredModelName,
        string? fallbackModelName,
        int maxTokens,
        decimal maxCostPerRequestUsd)
    {
        Guard.Against.NullOrWhiteSpace(preferredModelName);
        Guard.Against.NegativeOrZero(maxTokens);
        Guard.Against.Negative(maxCostPerRequestUsd, nameof(maxCostPerRequestUsd));

        return new ModelRoutingPolicy
        {
            Id = ModelRoutingPolicyId.New(),
            TenantId = tenantId,
            Intent = intent,
            PreferredModelName = preferredModelName,
            FallbackModelName = fallbackModelName,
            MaxTokens = maxTokens,
            MaxCostPerRequestUsd = maxCostPerRequestUsd,
            IsActive = true,
        };
    }

    /// <summary>Activa a política.</summary>
    public void Activate() => IsActive = true;

    /// <summary>Desactiva a política.</summary>
    public void Deactivate() => IsActive = false;
}

/// <summary>Identificador fortemente tipado de ModelRoutingPolicy.</summary>
public sealed record ModelRoutingPolicyId(Guid Value) : TypedIdBase(Value)
{
    public static ModelRoutingPolicyId New() => new(Guid.NewGuid());
    public static ModelRoutingPolicyId From(Guid id) => new(id);
}
