using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.Workflow.Persistence.Repositories;

/// <summary>
/// Repositório de políticas de SLA, implementando consultas específicas de negócio.
/// </summary>
internal sealed class SlaPolicyRepository(WorkflowDbContext context)
    : RepositoryBase<SlaPolicy, SlaPolicyId>(context), ISlaPolicyRepository
{
    /// <summary>Busca uma SlaPolicy pelo seu identificador.</summary>
    public override async Task<SlaPolicy?> GetByIdAsync(SlaPolicyId id, CancellationToken ct = default)
        => await context.SlaPolicies
            .SingleOrDefaultAsync(p => p.Id == id, ct);

    /// <summary>Lista políticas de SLA de um template de workflow.</summary>
    public async Task<IReadOnlyList<SlaPolicy>> GetByTemplateIdAsync(WorkflowTemplateId templateId, CancellationToken cancellationToken = default)
        => await context.SlaPolicies
            .Where(p => p.WorkflowTemplateId == templateId)
            .OrderBy(p => p.StageName)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Lista políticas de SLA com escalação habilitada.
    /// A verificação efetiva de violação de SLA requer cruzamento com WorkflowStage.StartedAt,
    /// que deve ser realizada na camada de Application.
    /// </summary>
    public async Task<IReadOnlyList<SlaPolicy>> ListExpiredAsync(CancellationToken cancellationToken = default)
        => await context.SlaPolicies
            .Where(p => p.EscalationEnabled)
            .ToListAsync(cancellationToken);
}
