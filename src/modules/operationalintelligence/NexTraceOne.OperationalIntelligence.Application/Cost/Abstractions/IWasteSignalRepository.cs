using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;

public interface IWasteSignalRepository
{
    Task<IReadOnlyList<WasteSignal>> ListByServiceAsync(string serviceName, string? environment = null, CancellationToken ct = default);
    Task<IReadOnlyList<WasteSignal>> ListAllAsync(string? teamName = null, bool includeAcknowledged = false, CancellationToken ct = default);
    Task AddAsync(WasteSignal signal, CancellationToken ct = default);
    Task<WasteSignal?> GetByIdAsync(WasteSignalId id, CancellationToken ct = default);
}
