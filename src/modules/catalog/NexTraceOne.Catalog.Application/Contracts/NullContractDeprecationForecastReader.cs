using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts;

/// <summary>
/// Implementação null (honest-null) de IContractDeprecationForecastReader.
/// Retorna lista vazia — sem dados de previsão de deprecação disponíveis.
/// Wave AV.3 — GetContractDeprecationForecast.
/// </summary>
public sealed class NullContractDeprecationForecastReader : IContractDeprecationForecastReader
{
    public Task<IReadOnlyList<IContractDeprecationForecastReader.ActiveContractForecastEntry>>
        ListActiveContractsByTenantAsync(string tenantId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IContractDeprecationForecastReader.ActiveContractForecastEntry>>([]);
}
