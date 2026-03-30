using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.Graph.Entities;

/// <summary>
/// Regra de matching automático que associa service.name de telemetria
/// a um ServiceAsset registado no catálogo.
/// Permite automação do fluxo de triagem de serviços descobertos.
/// Ex: regex "^payment-.*$" → ServiceAsset "Payment Service".
/// </summary>
public sealed class DiscoveryMatchRule : Entity<DiscoveryMatchRuleId>
{
    private DiscoveryMatchRule() { }

    /// <summary>Nome descritivo da regra.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Padrão regex para matching no service.name.</summary>
    public string Pattern { get; private set; } = string.Empty;

    /// <summary>Id do ServiceAsset alvo quando o padrão corresponde.</summary>
    public Guid TargetServiceAssetId { get; private set; }

    /// <summary>Prioridade da regra (menor = maior prioridade).</summary>
    public int Priority { get; private set; }

    /// <summary>Se a regra está ativa.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Data de criação UTC.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────

    /// <summary>Cria uma nova regra de matching automático.</summary>
    public static DiscoveryMatchRule Create(
        string name,
        string pattern,
        Guid targetServiceAssetId,
        int priority,
        DateTimeOffset createdAt)
    {
        return new DiscoveryMatchRule
        {
            Id = DiscoveryMatchRuleId.New(),
            Name = Guard.Against.NullOrWhiteSpace(name),
            Pattern = Guard.Against.NullOrWhiteSpace(pattern),
            TargetServiceAssetId = Guard.Against.Default(targetServiceAssetId),
            Priority = priority,
            IsActive = true,
            CreatedAt = createdAt
        };
    }

    // ── Mutations ─────────────────────────────────────────────────────

    /// <summary>Atualiza a regra.</summary>
    public void Update(string name, string pattern, Guid targetServiceAssetId, int priority)
    {
        Name = Guard.Against.NullOrWhiteSpace(name);
        Pattern = Guard.Against.NullOrWhiteSpace(pattern);
        TargetServiceAssetId = Guard.Against.Default(targetServiceAssetId);
        Priority = priority;
    }

    /// <summary>Ativa a regra.</summary>
    public void Activate() => IsActive = true;

    /// <summary>Desativa a regra.</summary>
    public void Deactivate() => IsActive = false;
}

/// <summary>Identificador fortemente tipado de DiscoveryMatchRule.</summary>
public sealed record DiscoveryMatchRuleId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static DiscoveryMatchRuleId New() => new(Guid.NewGuid());

    /// <summary>Cria a partir de Guid existente.</summary>
    public static DiscoveryMatchRuleId From(Guid value) => new(value);
}
