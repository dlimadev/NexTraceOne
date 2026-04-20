using Microsoft.EntityFrameworkCore;

using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de execuções de passos de runbook.
/// Persiste e consulta entidades RunbookStepExecution no IncidentDbContext.
/// </summary>
internal sealed class EfRunbookExecutionRepository(IncidentDbContext context) : IRunbookExecutionRepository
{
    /// <inheritdoc />
    public async Task AddAsync(RunbookStepExecution execution, CancellationToken ct)
    {
        context.RunbookStepExecutions.Add(execution);
        await context.CommitAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RunbookStepExecution>> GetByRunbookAsync(Guid runbookId, CancellationToken ct)
        => await context.RunbookStepExecutions
            .AsNoTracking()
            .Where(e => e.RunbookId == runbookId)
            .OrderByDescending(e => e.StartedAt)
            .ToListAsync(ct);
}
