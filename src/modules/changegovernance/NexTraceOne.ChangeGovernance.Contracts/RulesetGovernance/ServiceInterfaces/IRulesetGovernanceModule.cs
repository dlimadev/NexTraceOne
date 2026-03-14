namespace NexTraceOne.RulesetGovernance.Contracts.ServiceInterfaces;

/// <summary>
/// Interface pública do módulo RulesetGovernance.
/// Outros módulos que precisarem de dados deste módulo devem usar
/// este contrato — nunca acessar o DbContext ou repositórios diretamente.
/// </summary>
public interface IRulesetGovernanceModule
{
    /// <summary>Obtém o score de conformidade (linting) de uma release.</summary>
    Task<RulesetScoreDto?> GetRulesetScoreAsync(Guid releaseId, CancellationToken cancellationToken);

    /// <summary>Verifica se uma release passou no linting com score acima do limiar.</summary>
    Task<bool> IsReleaseCompliantAsync(Guid releaseId, decimal minimumScore, CancellationToken cancellationToken);
}

/// <summary>DTO de score de conformidade para comunicação entre módulos.</summary>
public sealed record RulesetScoreDto(
    Guid LintResultId,
    Guid ReleaseId,
    decimal Score,
    int TotalFindings,
    DateTimeOffset ExecutedAt);
