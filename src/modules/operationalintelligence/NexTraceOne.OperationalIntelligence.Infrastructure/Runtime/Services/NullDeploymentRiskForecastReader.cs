using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Services;

/// <summary>
/// Implementação honest-null de <see cref="IDeploymentRiskForecastReader"/>.
/// Retorna 0 (sem release de alto risco conhecida) quando o bridge com o módulo
/// ChangeGovernance não está configurado.
///
/// Wave AI.3 — GetIncidentProbabilityReport (OperationalIntelligence Runtime).
/// </summary>
internal sealed class NullDeploymentRiskForecastReader : IDeploymentRiskForecastReader
{
    /// <inheritdoc/>
    public Task<decimal> GetMaxRecentForecastRiskScoreAsync(
        string tenantId,
        string serviceName,
        int lookbackHours,
        CancellationToken cancellationToken = default)
        => Task.FromResult(0m);
}
