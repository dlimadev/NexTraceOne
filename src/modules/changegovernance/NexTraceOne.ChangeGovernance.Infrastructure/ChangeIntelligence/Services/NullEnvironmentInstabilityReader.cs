using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Services;

/// <summary>
/// Implementação honest-null de <see cref="IEnvironmentInstabilityReader"/>.
/// Retorna score 0 (sem instabilidade conhecida) quando o bridge com o módulo
/// OperationalIntelligence não está configurado.
///
/// Wave AI.1 — GetDeploymentRiskForecastReport (ChangeGovernance ChangeIntelligence).
/// </summary>
internal sealed class NullEnvironmentInstabilityReader : IEnvironmentInstabilityReader
{
    /// <inheritdoc/>
    public Task<decimal> GetInstabilityScoreAsync(
        string tenantId,
        string environment,
        CancellationToken cancellationToken = default)
        => Task.FromResult(0m);
}
