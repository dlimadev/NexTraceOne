using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts;

/// <summary>
/// Implementação null (honest-null) de <see cref="IContractTestReader"/>.
/// Retorna lista vazia — nenhum resultado de teste de contrato registado.
/// Wave AE.1 — GetContractTestCoverageReport.
/// </summary>
public sealed class NullContractTestReader : IContractTestReader
{
    public Task<IReadOnlyList<ContractTestEntry>> ListByTenantAsync(
        string tenantId,
        int lookbackDays,
        CancellationToken ct)
        => Task.FromResult<IReadOnlyList<ContractTestEntry>>([]);
}
