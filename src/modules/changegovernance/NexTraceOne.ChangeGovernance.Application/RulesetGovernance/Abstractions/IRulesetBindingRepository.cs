using NexTraceOne.ChangeGovernance.Domain.RulesetGovernance.Entities;

namespace NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Abstractions;

/// <summary>Contrato de repositório para a entidade RulesetBinding.</summary>
public interface IRulesetBindingRepository
{
    /// <summary>Busca um binding pelo identificador do ruleset e tipo de ativo.</summary>
    Task<RulesetBinding?> GetByRulesetAndAssetTypeAsync(RulesetId rulesetId, string assetType, CancellationToken cancellationToken = default);

    /// <summary>Adiciona um novo RulesetBinding ao repositório.</summary>
    void Add(RulesetBinding binding);
}
