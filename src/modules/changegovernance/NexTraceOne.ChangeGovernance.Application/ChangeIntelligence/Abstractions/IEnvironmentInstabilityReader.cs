namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>
/// Abstracção cross-module que permite ao módulo ChangeGovernance consultar o nível
/// de instabilidade de um ambiente a partir do módulo OperationalIntelligence.
///
/// Por omissão, é satisfeita por <c>NullEnvironmentInstabilityReader</c> que retorna 0
/// (honest-null pattern) — instabilidade não contabilizada sem bridge real configurado.
///
/// Wave AI.1 — GetDeploymentRiskForecastReport (ChangeGovernance ChangeIntelligence).
/// </summary>
public interface IEnvironmentInstabilityReader
{
    /// <summary>
    /// Retorna o score de instabilidade (0–100) para o ambiente indicado no tenant.
    /// Retorna 0 quando não há dados disponíveis ou o bridge não está configurado.
    /// Score mais alto indica ambiente mais instável (Unstable/Critical).
    /// </summary>
    Task<decimal> GetInstabilityScoreAsync(
        string tenantId,
        string environment,
        CancellationToken cancellationToken = default);
}
