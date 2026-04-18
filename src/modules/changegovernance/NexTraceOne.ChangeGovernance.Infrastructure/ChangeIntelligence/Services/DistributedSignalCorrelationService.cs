using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Application.Correlation;
using NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Services;

/// <summary>
/// Implementação de <see cref="IDistributedSignalCorrelationService"/> baseada
/// em <see cref="ChangeIntelligenceDbContext"/>.
///
/// Correlaciona releases e eventos de incidente para construir um score de risco
/// de promoção e identificar sinais de regressão por serviço e ambiente.
///
/// ISOLAMENTO: Toda correlação filtra por TenantId — nunca cruza tenants.
/// </summary>
internal sealed class DistributedSignalCorrelationService(
    ChangeIntelligenceDbContext context,
    ILogger<DistributedSignalCorrelationService> logger) : IDistributedSignalCorrelationService
{
    private const string IncidentCreatedEventType = "incident_created";

    /// <inheritdoc />
    public async Task<DistributedSignalCorrelation> CorrelateSignalsAsync(
        Guid tenantId,
        Guid environmentId,
        string serviceName,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Correlating signals for service '{ServiceName}', TenantId={TenantId}, EnvironmentId={EnvironmentId}, from={From:O}, to={To:O}",
            serviceName, tenantId, environmentId, from, to);

        var releaseIds = await context.Releases
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId
                && r.ServiceName == serviceName
                && r.EnvironmentId == environmentId
                && r.CreatedAt >= from
                && r.CreatedAt <= to)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var releaseCount = releaseIds.Count;

        var incidentCount = releaseCount > 0
            ? await context.ChangeEvents
                .AsNoTracking()
                .Where(e => releaseIds.Contains(e.ReleaseId)
                    && e.EventType == IncidentCreatedEventType
                    && e.OccurredAt >= from
                    && e.OccurredAt <= to)
                .CountAsync(cancellationToken)
            : 0;

        var correlationScore = incidentCount > 0
            ? Math.Min(1.0, (double)incidentCount / Math.Max(1, releaseCount))
            : 0.0;

        var hasPromotionRiskSignals = correlationScore > 0.2 || incidentCount > 2;

        var signals = BuildCorrelationSignals(serviceName, releaseCount, incidentCount, correlationScore);

        logger.LogInformation(
            "Signal correlation complete for '{ServiceName}': ReleaseCount={ReleaseCount}, IncidentCount={IncidentCount}, CorrelationScore={Score:F3}",
            serviceName, releaseCount, incidentCount, correlationScore);

        return new DistributedSignalCorrelation
        {
            TenantId = tenantId,
            EnvironmentId = environmentId,
            ServiceName = serviceName,
            From = from,
            To = to,
            ReleaseCount = releaseCount,
            IncidentCount = incidentCount,
            CorrelationScore = correlationScore,
            HasPromotionRiskSignals = hasPromotionRiskSignals,
            Signals = signals
        };
    }

    /// <inheritdoc />
    public async Task<EnvironmentSignalComparison> CompareEnvironmentsAsync(
        Guid tenantId,
        Guid sourceEnvironmentId,
        Guid targetEnvironmentId,
        string serviceName,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Comparing environments for service '{ServiceName}', source={SourceEnv}, target={TargetEnv}",
            serviceName, sourceEnvironmentId, targetEnvironmentId);

        var sourceCorrelation = await CorrelateSignalsAsync(
            tenantId, sourceEnvironmentId, serviceName, from, to, cancellationToken);

        var targetCorrelation = await CorrelateSignalsAsync(
            tenantId, targetEnvironmentId, serviceName, from, to, cancellationToken);

        var sourceIncidentRate = sourceCorrelation.ReleaseCount > 0
            ? (double)sourceCorrelation.IncidentCount / sourceCorrelation.ReleaseCount
            : 0.0;

        var targetIncidentRate = targetCorrelation.ReleaseCount > 0
            ? (double)targetCorrelation.IncidentCount / targetCorrelation.ReleaseCount
            : 0.0;

        // Divergence score: difference in incident rates, clamped to [0,1].
        var divergenceScore = Math.Min(1.0, Math.Abs(sourceIncidentRate - targetIncidentRate));

        // Regression: source environment has a higher incident rate than target.
        var hasRegression = sourceIncidentRate > targetIncidentRate && divergenceScore > 0.1;

        var divergenceSignals = BuildDivergenceSignals(
            serviceName, sourceCorrelation, targetCorrelation, sourceIncidentRate, targetIncidentRate, hasRegression);

        return new EnvironmentSignalComparison
        {
            TenantId = tenantId,
            SourceEnvironmentId = sourceEnvironmentId,
            TargetEnvironmentId = targetEnvironmentId,
            ServiceName = serviceName,
            HasRegression = hasRegression,
            DivergenceScore = divergenceScore,
            DivergenceSignals = divergenceSignals
        };
    }

    private static IReadOnlyList<string> BuildCorrelationSignals(
        string serviceName,
        int releaseCount,
        int incidentCount,
        double correlationScore)
    {
        var signals = new List<string>();

        if (releaseCount == 0)
        {
            signals.Add($"No releases found for '{serviceName}' in the specified window.");
            return signals;
        }

        if (incidentCount == 0)
        {
            signals.Add($"{releaseCount} release(s) analysed with no correlated incidents — low risk signal.");
            return signals;
        }

        signals.Add($"{incidentCount} incident event(s) correlated across {releaseCount} release(s) for '{serviceName}'.");

        if (correlationScore > 0.5)
            signals.Add($"High correlation score ({correlationScore:F2}): majority of releases have associated incidents.");
        else if (correlationScore > 0.2)
            signals.Add($"Moderate correlation score ({correlationScore:F2}): some releases have associated incidents.");

        if (incidentCount > 2)
            signals.Add($"Incident count ({incidentCount}) exceeds threshold — promotion risk elevated.");

        return signals;
    }

    private static IReadOnlyList<string> BuildDivergenceSignals(
        string serviceName,
        DistributedSignalCorrelation source,
        DistributedSignalCorrelation target,
        double sourceIncidentRate,
        double targetIncidentRate,
        bool hasRegression)
    {
        var signals = new List<string>();

        signals.Add(
            $"Source env: {source.ReleaseCount} release(s), {source.IncidentCount} incident(s) " +
            $"(rate: {sourceIncidentRate:F2}).");

        signals.Add(
            $"Target env: {target.ReleaseCount} release(s), {target.IncidentCount} incident(s) " +
            $"(rate: {targetIncidentRate:F2}).");

        if (hasRegression)
            signals.Add(
                $"Regression detected for '{serviceName}': source environment has higher incident rate " +
                $"({sourceIncidentRate:F2}) than target ({targetIncidentRate:F2}).");

        return signals;
    }
}
