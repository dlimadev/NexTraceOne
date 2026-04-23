using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts;

/// <summary>
/// Implementação null (honest-null) de IExperimentGovernanceReader.
/// Retorna lista vazia — sem dados de experimentos disponíveis.
/// Wave AS.3 — GetExperimentGovernanceReport.
/// </summary>
public sealed class NullExperimentGovernanceReader : IExperimentGovernanceReader
{
    public Task<IReadOnlyList<IExperimentGovernanceReader.ExperimentEntry>> ListExperimentsByTenantAsync(
        string tenantId,
        CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IExperimentGovernanceReader.ExperimentEntry>>([]);
}
