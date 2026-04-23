using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts;

/// <summary>
/// Implementação honest-null de <see cref="ITrafficObservationReader"/>.
/// Retorna colecções vazias até a infraestrutura real ser ligada.
/// </summary>
public sealed class NullTrafficObservationReader : ITrafficObservationReader
{
    public Task<IReadOnlyList<ITrafficObservationReader.ServiceTrafficObservationEntry>> ListByTenantAsync(
        string tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ITrafficObservationReader.ServiceTrafficObservationEntry>>([]);

    public Task<IReadOnlyList<ITrafficObservationReader.DailyDeviationSnapshot>> GetDeviationTrendAsync(
        string tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ITrafficObservationReader.DailyDeviationSnapshot>>([]);
}
