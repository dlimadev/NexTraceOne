using NexTraceOne.BuildingBlocks.Observability.Observability.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Services;

/// <summary>
/// Provider de discovery que consulta traces OTel via IObservabilityProvider
/// para extrair service.name distintos.
/// 
/// Esta implementação usa o IObservabilityProvider já existente e extrai
/// os serviços distintos dos traces. Numa fase futura pode ser substituída
/// por queries diretas a ClickHouse para melhor performance.
/// </summary>
internal sealed class OtelServiceDiscoveryProvider(IObservabilityProvider observabilityProvider) : IServiceDiscoveryProvider
{
    public async Task<IReadOnlyList<DiscoveredServiceInfo>> DiscoverServicesAsync(
        string environment,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken)
    {
        var filter = new NexTraceOne.BuildingBlocks.Observability.Observability.Models.TraceQueryFilter
        {
            Environment = environment,
            From = from,
            Until = until
        };

        var traces = await observabilityProvider.QueryTracesAsync(filter, cancellationToken);

        // Agrupa traces por serviço para obter contagens
        var grouped = traces
            .Where(t => !string.IsNullOrWhiteSpace(t.ServiceName))
            .GroupBy(t => t.ServiceName)
            .Select(g => new DiscoveredServiceInfo(
                ServiceName: g.Key,
                ServiceNamespace: string.Empty,
                TraceCount: g.Count(),
                EndpointCount: g.Select(t => t.OperationName).Distinct().Count(),
                FirstSeen: g.Min(t => t.StartTime),
                LastSeen: g.Max(t => t.StartTime)))
            .ToList();

        return grouped;
    }
}
