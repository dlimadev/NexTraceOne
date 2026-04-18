using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Correlation;
using NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Services;

/// <summary>
/// Implementação de <see cref="IPromotionRiskSignalProvider"/> baseada em
/// <see cref="ChangeIntelligenceDbContext"/>.
///
/// Agrega sinais de releases, rollbacks e eventos de incidente no ambiente source
/// para construir uma avaliação de risco antes de promover para o ambiente target.
///
/// NEUTRALIDADE: Esta implementação apenas fornece sinais — não bloqueia promoções.
/// A decisão de bloqueio é interpretada pelo <see cref="PromotionRiskAssessment.ShouldBlock"/>.
///
/// ISOLAMENTO: Toda consulta filtra por TenantId — nunca cruza tenants.
/// </summary>
internal sealed class PromotionRiskSignalProvider(
    ChangeIntelligenceDbContext context,
    IDateTimeProvider clock,
    ILogger<PromotionRiskSignalProvider> logger) : IPromotionRiskSignalProvider
{
    private const string IncidentCreatedEventType = "incident_created";

    /// <inheritdoc />
    public async Task<PromotionRiskAssessment> AssessPromotionRiskAsync(
        Guid tenantId,
        Guid sourceEnvironmentId,
        Guid targetEnvironmentId,
        string? serviceName,
        DateTimeOffset since,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Assessing promotion risk: TenantId={TenantId}, SourceEnv={SourceEnv}, TargetEnv={TargetEnv}, Service='{ServiceName}', Since={Since:O}",
            tenantId, sourceEnvironmentId, targetEnvironmentId, serviceName ?? "(all)", since);

        var releaseQuery = context.Releases
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId
                && r.EnvironmentId == sourceEnvironmentId
                && r.CreatedAt >= since);

        if (!string.IsNullOrWhiteSpace(serviceName))
            releaseQuery = releaseQuery.Where(r => r.ServiceName == serviceName);

        var releases = await releaseQuery
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        var releaseIds = releases.Select(r => r.Id).ToList();

        var incidentCount = releaseIds.Count > 0
            ? await context.ChangeEvents
                .AsNoTracking()
                .Where(e => releaseIds.Contains(e.ReleaseId)
                    && e.EventType == IncidentCreatedEventType)
                .CountAsync(cancellationToken)
            : 0;

        // Identify rollback releases (releases that were themselves triggered as rollbacks).
        var rollbackReleases = releases
            .Where(r => r.RolledBackFromReleaseId is not null)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        var rollbackCount = rollbackReleases.Count;
        var mostRecentRollback = rollbackReleases.FirstOrDefault();
        var mostRecentRollbackAge = mostRecentRollback is not null
            ? clock.UtcNow - mostRecentRollback.CreatedAt
            : (TimeSpan?)null;

        var (riskLevel, riskScore) = ComputeRiskLevel(
            incidentCount, rollbackCount, mostRecentRollbackAge);

        var signals = BuildSignals(
            incidentCount, rollbackCount, mostRecentRollbackAge, riskLevel, releases.Count);

        logger.LogInformation(
            "Promotion risk assessment: TenantId={TenantId}, Service='{ServiceName}', RiskLevel={RiskLevel}, IncidentCount={IncidentCount}, RollbackCount={RollbackCount}",
            tenantId, serviceName ?? "(all)", riskLevel, incidentCount, rollbackCount);

        return new PromotionRiskAssessment
        {
            TenantId = tenantId,
            SourceEnvironmentId = sourceEnvironmentId,
            TargetEnvironmentId = targetEnvironmentId,
            ServiceName = serviceName,
            AssessedAt = clock.UtcNow,
            RiskLevel = riskLevel,
            RiskScore = riskScore,
            Signals = signals
        };
    }

    private static (PromotionRiskLevel Level, double Score) ComputeRiskLevel(
        int incidentCount,
        int rollbackCount,
        TimeSpan? mostRecentRollbackAge)
    {
        // Critical: 5+ incidents OR rollback within last 24 hours.
        if (incidentCount >= 5
            || (mostRecentRollbackAge.HasValue && mostRecentRollbackAge.Value.TotalHours <= 24))
        {
            return (PromotionRiskLevel.Critical, 1.0);
        }

        // High: 3-4 incidents OR rollback within last 3 days.
        if (incidentCount >= 3
            || (mostRecentRollbackAge.HasValue && mostRecentRollbackAge.Value.TotalDays <= 3))
        {
            return (PromotionRiskLevel.High, 0.75);
        }

        // Medium: 2 incidents OR rollback within last 7 days.
        if (incidentCount >= 2
            || (mostRecentRollbackAge.HasValue && mostRecentRollbackAge.Value.TotalDays <= 7))
        {
            return (PromotionRiskLevel.Medium, 0.5);
        }

        // Low: 1 incident OR any rollback older than 7 days.
        if (incidentCount >= 1 || rollbackCount > 0)
        {
            return (PromotionRiskLevel.Low, 0.25);
        }

        return (PromotionRiskLevel.None, 0.0);
    }

    private static IReadOnlyList<PromotionRiskSignal> BuildSignals(
        int incidentCount,
        int rollbackCount,
        TimeSpan? mostRecentRollbackAge,
        PromotionRiskLevel riskLevel,
        int releaseCount)
    {
        var signals = new List<PromotionRiskSignal>();

        if (releaseCount == 0)
        {
            signals.Add(new PromotionRiskSignal
            {
                SignalType = "no_releases",
                Description = "No releases found in the source environment for the specified window.",
                Severity = PromotionRiskLevel.None,
                Module = "ChangeIntelligence"
            });
            return signals;
        }

        if (incidentCount > 0)
        {
            var incidentSeverity = incidentCount >= 5 ? PromotionRiskLevel.Critical
                : incidentCount >= 3 ? PromotionRiskLevel.High
                : incidentCount >= 2 ? PromotionRiskLevel.Medium
                : PromotionRiskLevel.Low;

            signals.Add(new PromotionRiskSignal
            {
                SignalType = "incident_correlation",
                Description = $"{incidentCount} incident(s) correlated with releases in the source environment " +
                              $"since the assessment window opened.",
                Severity = incidentSeverity,
                Module = "ChangeIntelligence"
            });
        }

        if (rollbackCount > 0 && mostRecentRollbackAge.HasValue)
        {
            var rollbackSeverity = mostRecentRollbackAge.Value.TotalHours <= 24 ? PromotionRiskLevel.Critical
                : mostRecentRollbackAge.Value.TotalDays <= 3 ? PromotionRiskLevel.High
                : mostRecentRollbackAge.Value.TotalDays <= 7 ? PromotionRiskLevel.Medium
                : PromotionRiskLevel.Low;

            signals.Add(new PromotionRiskSignal
            {
                SignalType = "recent_rollback",
                Description = $"{rollbackCount} rollback(s) detected in the source environment. " +
                              $"Most recent was {FormatAge(mostRecentRollbackAge.Value)} ago.",
                Severity = rollbackSeverity,
                Module = "ChangeIntelligence"
            });
        }

        if (signals.Count == 0)
        {
            signals.Add(new PromotionRiskSignal
            {
                SignalType = "clean_window",
                Description = $"{releaseCount} release(s) assessed with no incident or rollback signals detected.",
                Severity = PromotionRiskLevel.None,
                Module = "ChangeIntelligence"
            });
        }

        return signals;
    }

    private static string FormatAge(TimeSpan age)
    {
        if (age.TotalHours < 1) return $"{(int)age.TotalMinutes}m";
        if (age.TotalDays < 1) return $"{(int)age.TotalHours}h";
        return $"{(int)age.TotalDays}d";
    }
}
