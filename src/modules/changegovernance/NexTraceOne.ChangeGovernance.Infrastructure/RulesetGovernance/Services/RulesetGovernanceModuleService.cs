using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NexTraceOne.ChangeGovernance.Contracts.RulesetGovernance.ServiceInterfaces;
using NexTraceOne.ChangeGovernance.Infrastructure.RulesetGovernance.Persistence;

namespace NexTraceOne.ChangeGovernance.Infrastructure.RulesetGovernance.Services;

/// <summary>
/// Implementação do contrato público do módulo RulesetGovernance.
/// Outros módulos consomem esta interface — nunca o DbContext ou repositórios diretamente.
/// </summary>
internal sealed class RulesetGovernanceModuleService(
    RulesetGovernanceDbContext context,
    ILogger<RulesetGovernanceModuleService> logger) : IRulesetGovernanceModule
{
    /// <inheritdoc />
    public async Task<RulesetScoreDto?> GetRulesetScoreAsync(
        Guid releaseId,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Fetching ruleset score for release {ReleaseId}", releaseId);

        return await context.LintResults
            .AsNoTracking()
            .Where(lr => lr.ReleaseId == releaseId)
            .OrderByDescending(lr => lr.ExecutedAt)
            .Select(lr => new RulesetScoreDto(
                lr.Id.Value,
                lr.ReleaseId,
                lr.Score,
                lr.TotalFindings,
                lr.ExecutedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LintViolationSummaryDto>> GetRecentViolationsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        int maxCount = 50,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Fetching recent lint violations between {From} and {To}", from, to);

        return await context.LintResults
            .AsNoTracking()
            .Where(lr => lr.ExecutedAt >= from && lr.ExecutedAt <= to && lr.TotalFindings > 0)
            .OrderByDescending(lr => lr.ExecutedAt)
            .Take(maxCount)
            .Select(lr => new LintViolationSummaryDto(
                lr.Id.Value,
                lr.ReleaseId,
                lr.Score,
                lr.TotalFindings,
                lr.ExecutedAt))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsReleaseCompliantAsync(
        Guid releaseId,
        decimal minimumScore,
        CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Checking release compliance for release {ReleaseId} with minimum score {MinimumScore}",
            releaseId, minimumScore);

        return await context.LintResults
            .AsNoTracking()
            .Where(lr => lr.ReleaseId == releaseId)
            .OrderByDescending(lr => lr.ExecutedAt)
            .Select(lr => (decimal?)lr.Score)
            .FirstOrDefaultAsync(cancellationToken) >= minimumScore;
    }
}
