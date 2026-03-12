using NexTraceOne.RulesetGovernance.Domain.Entities;

namespace NexTraceOne.RulesetGovernance.Application.Abstractions;

/// <summary>Contrato de repositório para a entidade Ruleset.</summary>
public interface IRulesetRepository
{
    /// <summary>Busca um Ruleset pelo seu identificador.</summary>
    Task<Ruleset?> GetByIdAsync(RulesetId id, CancellationToken cancellationToken = default);

    /// <summary>Lista rulesets ativos com paginação.</summary>
    Task<IReadOnlyList<Ruleset>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Conta o total de rulesets ativos.</summary>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>Adiciona um novo Ruleset ao repositório.</summary>
    void Add(Ruleset ruleset);
}
