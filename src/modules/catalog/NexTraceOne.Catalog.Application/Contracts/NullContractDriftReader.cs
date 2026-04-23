using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts;

/// <summary>
/// Implementação null (honest-null) de IContractDriftReader.
/// Retorna lista vazia — sem dados de runtime disponíveis, sem divergências detectadas.
/// Wave AM.2 — GetContractDriftFromRealityReport.
/// </summary>
public sealed class NullContractDriftReader : IContractDriftReader
{
    public Task<IReadOnlyList<IContractDriftReader.ContractRuntimeObservation>> ListByTenantAsync(
        string tenantId, int lookbackDays, int unusedOpsStagnationDays, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IContractDriftReader.ContractRuntimeObservation>>([]);
}
