using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.Workflow.Persistence.Repositories;

/// <summary>
/// Repositório de decisões de aprovação, implementando consultas específicas de negócio.
/// </summary>
internal sealed class ApprovalDecisionRepository(WorkflowDbContext context)
    : RepositoryBase<ApprovalDecision, ApprovalDecisionId>(context), IApprovalDecisionRepository
{
    /// <summary>Busca uma ApprovalDecision pelo seu identificador.</summary>
    public override async Task<ApprovalDecision?> GetByIdAsync(ApprovalDecisionId id, CancellationToken ct = default)
        => await context.ApprovalDecisions
            .SingleOrDefaultAsync(d => d.Id == id, ct);

    /// <summary>Lista todas as decisões de um estágio de workflow.</summary>
    public async Task<IReadOnlyList<ApprovalDecision>> ListByStageIdAsync(WorkflowStageId stageId, CancellationToken cancellationToken = default)
        => await context.ApprovalDecisions
            .Where(d => d.WorkflowStageId == stageId)
            .OrderBy(d => d.DecidedAt)
            .ToListAsync(cancellationToken);

    /// <summary>Lista todas as decisões de uma instância de workflow.</summary>
    public async Task<IReadOnlyList<ApprovalDecision>> ListByInstanceIdAsync(WorkflowInstanceId instanceId, CancellationToken cancellationToken = default)
        => await context.ApprovalDecisions
            .Where(d => d.WorkflowInstanceId == instanceId)
            .OrderBy(d => d.DecidedAt)
            .ToListAsync(cancellationToken);
}
