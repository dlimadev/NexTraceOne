using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime;

/// <summary>
/// Implementação honest-null de <see cref="ITrafficAnomalyReader"/>.
/// Retorna colecções vazias até a infraestrutura real ser ligada.
/// </summary>
public sealed class NullTrafficAnomalyReader : ITrafficAnomalyReader
{
    public Task<IReadOnlyList<ITrafficAnomalyReader.ServiceTrafficAnomalyEntry>> ListByTenantAsync(
        string tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ITrafficAnomalyReader.ServiceTrafficAnomalyEntry>>([]);

    public Task<IReadOnlyList<ITrafficAnomalyReader.TimelineEvent>> GetTimelineEventsAsync(
        string tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ITrafficAnomalyReader.TimelineEvent>>([]);
}
