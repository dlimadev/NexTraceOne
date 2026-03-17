using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.RulesetGovernance.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.RulesetGovernance.Persistence.Repositories;

/// <summary>
/// Repositorio de resultados de linting.
/// </summary>
internal sealed class LintResultRepository(RulesetGovernanceDbContext context)
    : RepositoryBase<LintResult, LintResultId>(context), ILintResultRepository
{
    /// <summary>Busca o resultado de linting pelo identificador da release.</summary>
    public async Task<LintResult?> GetByReleaseIdAsync(Guid releaseId, CancellationToken cancellationToken = default)
        => await context.LintResults
            .SingleOrDefaultAsync(lr => lr.ReleaseId == releaseId, cancellationToken);
}
