using Microsoft.EntityFrameworkCore;

using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de MitigationValidationLog.
/// Persiste e lê registos de validação pós-mitigação a partir de IncidentDbContext (tabela ops_mitigation_validations).
/// </summary>
public sealed class EfMitigationValidationRepository(IncidentDbContext db) : IMitigationValidationRepository
{
    /// <inheritdoc/>
    public async Task AddAsync(MitigationValidationLog log, CancellationToken cancellationToken = default)
    {
        db.MitigationValidations.Add(log);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MitigationValidationLog>> GetByWorkflowIdAsync(
        Guid workflowId, CancellationToken cancellationToken = default)
        => await db.MitigationValidations
            .Where(v => v.WorkflowId == workflowId)
            .OrderByDescending(v => v.ValidatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
}
