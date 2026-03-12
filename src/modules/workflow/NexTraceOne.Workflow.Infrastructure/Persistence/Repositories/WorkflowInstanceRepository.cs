using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Workflow.Application.Abstractions;
using NexTraceOne.Workflow.Domain.Entities;
using NexTraceOne.Workflow.Domain.Enums;

namespace NexTraceOne.Workflow.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de instâncias de workflow, implementando consultas específicas de negócio.
/// </summary>
internal sealed class WorkflowInstanceRepository(WorkflowDbContext context)
    : RepositoryBase<WorkflowInstance, WorkflowInstanceId>(context), IWorkflowInstanceRepository
{
    /// <summary>Busca uma WorkflowInstance pelo seu identificador.</summary>
    public override async Task<WorkflowInstance?> GetByIdAsync(WorkflowInstanceId id, CancellationToken ct = default)
        => await context.WorkflowInstances
            .SingleOrDefaultAsync(i => i.Id == id, ct);

    /// <summary>Busca instância de workflow pela release associada.</summary>
    public async Task<WorkflowInstance?> GetByReleaseIdAsync(Guid releaseId, CancellationToken cancellationToken = default)
        => await context.WorkflowInstances
            .SingleOrDefaultAsync(i => i.ReleaseId == releaseId, cancellationToken);

    /// <summary>Lista instâncias de workflow por status com paginação.</summary>
    public async Task<IReadOnlyList<WorkflowInstance>> ListByStatusAsync(WorkflowStatus status, int page, int pageSize, CancellationToken cancellationToken = default)
        => await context.WorkflowInstances
            .Where(i => i.Status == status)
            .OrderByDescending(i => i.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    /// <summary>Conta o total de instâncias de workflow com o status informado.</summary>
    public async Task<int> CountByStatusAsync(WorkflowStatus status, CancellationToken cancellationToken = default)
        => await context.WorkflowInstances
            .CountAsync(i => i.Status == status, cancellationToken);
}
