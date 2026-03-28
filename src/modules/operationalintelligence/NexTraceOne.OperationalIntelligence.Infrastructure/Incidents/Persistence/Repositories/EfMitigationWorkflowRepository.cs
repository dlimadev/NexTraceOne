using Microsoft.EntityFrameworkCore;

using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de MitigationWorkflowRecord.
/// Persiste e lê workflows de mitigação a partir de IncidentDbContext (tabela ops_mitigation_workflows).
/// </summary>
public sealed class EfMitigationWorkflowRepository(IncidentDbContext db) : IMitigationWorkflowRepository
{
    /// <inheritdoc/>
    public async Task AddAsync(MitigationWorkflowRecord record, CancellationToken cancellationToken = default)
    {
        db.MitigationWorkflows.Add(record);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<MitigationWorkflowRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await db.MitigationWorkflows
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == MitigationWorkflowRecordId.From(id), cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MitigationWorkflowRecord>> GetByIncidentIdAsync(
        string incidentId, CancellationToken cancellationToken = default)
        => await db.MitigationWorkflows
            .Where(w => w.IncidentId == incidentId)
            .OrderByDescending(w => w.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
}
