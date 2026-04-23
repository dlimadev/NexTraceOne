using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts;

/// <summary>
/// Implementação honest-null de <see cref="IHighTrafficEndpointReader"/>.
/// Retorna colecções vazias até a infraestrutura real ser ligada.
/// </summary>
public sealed class NullHighTrafficEndpointReader : IHighTrafficEndpointReader
{
    public Task<IReadOnlyList<IHighTrafficEndpointReader.EndpointTrafficEntry>> ListByTenantAsync(
        string tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<IHighTrafficEndpointReader.EndpointTrafficEntry>>([]);
}
