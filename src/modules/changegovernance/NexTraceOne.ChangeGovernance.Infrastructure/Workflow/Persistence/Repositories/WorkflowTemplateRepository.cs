using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Workflow.Application.Abstractions;
using NexTraceOne.Workflow.Domain.Entities;

namespace NexTraceOne.Workflow.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de templates de workflow, implementando consultas específicas de negócio.
/// </summary>
internal sealed class WorkflowTemplateRepository(WorkflowDbContext context)
    : RepositoryBase<WorkflowTemplate, WorkflowTemplateId>(context), IWorkflowTemplateRepository
{
    /// <summary>Busca um WorkflowTemplate pelo seu identificador.</summary>
    public override async Task<WorkflowTemplate?> GetByIdAsync(WorkflowTemplateId id, CancellationToken ct = default)
        => await context.WorkflowTemplates
            .SingleOrDefaultAsync(t => t.Id == id, ct);

    /// <summary>Busca templates ativos pelo tipo de mudança.</summary>
    public async Task<IReadOnlyList<WorkflowTemplate>> GetByChangeTypeAsync(string changeType, CancellationToken cancellationToken = default)
        => await context.WorkflowTemplates
            .Where(t => t.IsActive && t.ChangeType == changeType)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

    /// <summary>Lista templates ativos com paginação.</summary>
    public async Task<IReadOnlyList<WorkflowTemplate>> ListActiveAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        => await context.WorkflowTemplates
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    /// <summary>Conta o total de templates ativos.</summary>
    public async Task<int> CountActiveAsync(CancellationToken cancellationToken = default)
        => await context.WorkflowTemplates
            .CountAsync(t => t.IsActive, cancellationToken);
}
