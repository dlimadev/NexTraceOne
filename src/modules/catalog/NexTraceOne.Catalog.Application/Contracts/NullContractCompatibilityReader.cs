using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts;

/// <summary>
/// Implementação null (honest-null) de <see cref="IContractCompatibilityReader"/>.
/// Retorna lista vazia — nenhum histórico de compatibilidade de contrato registado.
/// Wave AE.3 — GetApiBackwardCompatibilityReport.
/// </summary>
public sealed class NullContractCompatibilityReader : IContractCompatibilityReader
{
    public Task<IReadOnlyList<ContractCompatibilityEntry>> ListByTenantAsync(
        string tenantId,
        int lookbackDays,
        CancellationToken ct)
        => Task.FromResult<IReadOnlyList<ContractCompatibilityEntry>>([]);
}
