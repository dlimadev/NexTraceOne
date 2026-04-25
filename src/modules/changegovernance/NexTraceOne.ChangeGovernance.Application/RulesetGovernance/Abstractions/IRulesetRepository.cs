using NexTraceOne.ChangeGovernance.Domain.RulesetGovernance.Entities;

namespace NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Abstractions;

/// <summary>Contrato de repositório para a entidade Ruleset.</summary>
public interface IRulesetRepository
{
    /// <summary>Busca um Ruleset pelo seu identificador.</summary>
    Task<Ruleset?> GetByIdAsync(RulesetId id, CancellationToken cancellationToken = default);

    /// <summary>Lista rulesets ativos com paginação.</summary>
    Task<IReadOnlyList<Ruleset>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Conta o total de rulesets ativos.</summary>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>Busca um Ruleset pelo nome (para idempotência no marketplace).</summary>
    Task<Ruleset?> FindByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Adiciona um novo Ruleset ao repositório.</summary>
    void Add(Ruleset ruleset);

    /// <summary>Remove um Ruleset do repositório.</summary>
    void Remove(Ruleset ruleset);
}
