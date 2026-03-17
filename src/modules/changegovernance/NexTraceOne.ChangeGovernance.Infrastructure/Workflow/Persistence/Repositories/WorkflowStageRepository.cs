using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.Workflow.Persistence.Repositories;

/// <summary>
/// Repositório de estágios de workflow, implementando consultas específicas de negócio.
/// </summary>
internal sealed class WorkflowStageRepository(WorkflowDbContext context)
    : RepositoryBase<WorkflowStage, WorkflowStageId>(context), IWorkflowStageRepository
{
    /// <summary>Busca um WorkflowStage pelo seu identificador.</summary>
    public override async Task<WorkflowStage?> GetByIdAsync(WorkflowStageId id, CancellationToken ct = default)
        => await context.WorkflowStages
            .SingleOrDefaultAsync(s => s.Id == id, ct);

    /// <summary>Lista todos os estágios de uma instância de workflow ordenados pela ordem sequencial.</summary>
    public async Task<IReadOnlyList<WorkflowStage>> ListByInstanceIdAsync(WorkflowInstanceId instanceId, CancellationToken cancellationToken = default)
        => await context.WorkflowStages
            .Where(s => s.WorkflowInstanceId == instanceId)
            .OrderBy(s => s.StageOrder)
            .ToListAsync(cancellationToken);
}
