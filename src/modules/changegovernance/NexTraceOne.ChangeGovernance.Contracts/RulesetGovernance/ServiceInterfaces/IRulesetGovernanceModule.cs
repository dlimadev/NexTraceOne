namespace NexTraceOne.ChangeGovernance.Contracts.RulesetGovernance.ServiceInterfaces;

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

    /// <summary>
    /// Retorna resultados de linting com violations (TotalFindings > 0) numa janela temporal.
    /// Usado por anotações de dashboard para sobreposição em séries temporais.
    /// </summary>
    Task<IReadOnlyList<LintViolationSummaryDto>> GetRecentViolationsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        int maxCount = 50,
        CancellationToken cancellationToken = default);
}

/// <summary>Sumário de violation de linting para consumo cross-module em dashboards e anotações.</summary>
public sealed record LintViolationSummaryDto(
    Guid LintResultId,
    Guid ReleaseId,
    decimal Score,
    int TotalFindings,
    DateTimeOffset ExecutedAt);

/// <summary>DTO de score de conformidade para comunicação entre módulos.</summary>
public sealed record RulesetScoreDto(
    Guid LintResultId,
    Guid ReleaseId,
    decimal Score,
    int TotalFindings,
    DateTimeOffset ExecutedAt);
