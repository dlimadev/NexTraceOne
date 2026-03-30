namespace NexTraceOne.Catalog.Application.Graph.Abstractions;

/// <summary>
/// Provider para descoberta de serviços a partir de fontes de telemetria.
/// Abstrai a consulta a Elastic, ClickHouse ou outra fonte.
/// A implementação concreta reside na camada de Infrastructure.
/// </summary>
public interface IServiceDiscoveryProvider
{
    /// <summary>
    /// Consulta a fonte de telemetria e retorna os serviços distintos observados
    /// num determinado ambiente e janela temporal.
    /// </summary>
    Task<IReadOnlyList<DiscoveredServiceInfo>> DiscoverServicesAsync(
        string environment,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken);
}

/// <summary>
/// Informação resumida de um serviço descoberto pela telemetria.
/// </summary>
public sealed record DiscoveredServiceInfo(
    string ServiceName,
    string ServiceNamespace,
    long TraceCount,
    int EndpointCount,
    DateTimeOffset FirstSeen,
    DateTimeOffset LastSeen);
