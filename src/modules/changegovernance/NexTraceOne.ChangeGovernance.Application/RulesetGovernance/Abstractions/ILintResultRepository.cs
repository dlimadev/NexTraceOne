using NexTraceOne.RulesetGovernance.Domain.Entities;

namespace NexTraceOne.RulesetGovernance.Application.Abstractions;

/// <summary>Contrato de repositório para a entidade LintResult.</summary>
public interface ILintResultRepository
{
    /// <summary>Busca o resultado de linting pelo identificador da release.</summary>
    Task<LintResult?> GetByReleaseIdAsync(Guid releaseId, CancellationToken cancellationToken = default);

    /// <summary>Adiciona um novo LintResult ao repositório.</summary>
    void Add(LintResult lintResult);
}
