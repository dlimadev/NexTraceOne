using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts;

/// <summary>
/// Implementação null (honest-null) de IApiVersionStrategyReader.
/// Retorna lista vazia — sem dados de versionamento disponíveis.
/// Wave AV.2 — GetApiVersionStrategyReport.
/// </summary>
public sealed class NullApiVersionStrategyReader : IApiVersionStrategyReader
{
    public Task<IReadOnlyList<IApiVersionStrategyReader.ServiceVersionEntry>>
        ListServiceVersionDataByTenantAsync(string tenantId, int lookbackDays, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IApiVersionStrategyReader.ServiceVersionEntry>>([]);
}
