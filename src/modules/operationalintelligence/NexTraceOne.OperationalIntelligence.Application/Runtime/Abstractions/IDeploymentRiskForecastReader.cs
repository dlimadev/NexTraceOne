namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

/// <summary>
/// Abstracção cross-module que permite ao módulo OperationalIntelligence consultar
/// o score de risco de forecast de uma release recente do módulo ChangeGovernance.
///
/// Por omissão, é satisfeita por <c>NullDeploymentRiskForecastReader</c> que retorna 0
/// (honest-null pattern) — sinal RecentHighRiskRelease não contabilizado sem bridge real.
///
/// Wave AI.3 — GetIncidentProbabilityReport (OperationalIntelligence Runtime).
/// </summary>
public interface IDeploymentRiskForecastReader
{
    /// <summary>
    /// Verifica se existe pelo menos uma release recente (últimas 24h) com ForecastRiskScore
    /// acima do threshold indicado para o serviço especificado.
    /// Retorna 0 quando não há dados ou o bridge não está configurado.
    /// Retorna o score mais alto encontrado no período (0–100).
    /// </summary>
    Task<decimal> GetMaxRecentForecastRiskScoreAsync(
        string tenantId,
        string serviceName,
        int lookbackHours,
        CancellationToken cancellationToken = default);
}
