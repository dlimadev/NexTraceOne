using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence;

/// <summary>
/// Implementação null (honest-null) de IDeploymentFrequencyReader.
/// Retorna lista vazia — sem dados de frequência de deployments disponíveis.
/// Wave AW.3 — GetDeploymentFrequencyHealthReport.
/// </summary>
public sealed class NullDeploymentFrequencyReader : IDeploymentFrequencyReader
{
    /// <inheritdoc />
    public Task<IReadOnlyList<DeploymentFrequencyEntry>> ListDeploymentsByTenantAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
        => Task.FromResult<IReadOnlyList<DeploymentFrequencyEntry>>([]);
}
