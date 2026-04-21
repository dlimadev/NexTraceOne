using NexTraceOne.Knowledge.Domain.Entities;

namespace NexTraceOne.Knowledge.Application.Abstractions;

public interface IProposedRunbookRepository
{
    Task AddAsync(ProposedRunbook runbook, CancellationToken ct = default);
    Task<ProposedRunbook?> GetByIdAsync(ProposedRunbookId id, CancellationToken ct = default);
    Task<IReadOnlyList<ProposedRunbook>> ListAsync(ProposedRunbookStatus? status = null, string? serviceName = null, CancellationToken ct = default);
    Task<ProposedRunbook?> GetByIncidentIdAsync(Guid incidentId, CancellationToken ct = default);
}
