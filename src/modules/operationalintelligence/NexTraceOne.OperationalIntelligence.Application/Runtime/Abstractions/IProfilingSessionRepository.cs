using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

/// <summary>
/// Contrato de repositório para sessões de profiling contínuo.
/// </summary>
public interface IProfilingSessionRepository
{
    Task<ProfilingSession?> GetByIdAsync(ProfilingSessionId id, CancellationToken ct = default);

    Task<IReadOnlyList<ProfilingSession>> ListByServiceAsync(
        string serviceName,
        string environment,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<ProfilingSession?> GetLatestByServiceAsync(
        string serviceName,
        string environment,
        CancellationToken ct = default);

    void Add(ProfilingSession session);
    void Update(ProfilingSession session);
}
