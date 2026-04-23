using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts;

/// <summary>
/// Implementação null (honest-null) de IContractDeprecationPipelineReader.
/// Retorna lista vazia — sem dados de pipeline de deprecação disponíveis.
/// Wave AV.1 — GetContractDeprecationPipelineReport.
/// </summary>
public sealed class NullContractDeprecationPipelineReader : IContractDeprecationPipelineReader
{
    public Task<IReadOnlyList<IContractDeprecationPipelineReader.DeprecatedContractEntry>>
        ListDeprecatedContractsByTenantAsync(string tenantId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IContractDeprecationPipelineReader.DeprecatedContractEntry>>([]);
}
