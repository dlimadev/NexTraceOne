using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.Workflow.Persistence.Repositories;

/// <summary>
/// Repositório de pacotes de evidência, implementando consultas específicas de negócio.
/// </summary>
internal sealed class EvidencePackRepository(WorkflowDbContext context)
    : RepositoryBase<EvidencePack, EvidencePackId>(context), IEvidencePackRepository
{
    /// <summary>Busca um EvidencePack pelo seu identificador.</summary>
    public override async Task<EvidencePack?> GetByIdAsync(EvidencePackId id, CancellationToken ct = default)
        => await context.EvidencePacks
            .SingleOrDefaultAsync(e => e.Id == id, ct);

    /// <summary>Busca o EvidencePack associado a uma instância de workflow.</summary>
    public async Task<EvidencePack?> GetByWorkflowInstanceIdAsync(WorkflowInstanceId instanceId, CancellationToken cancellationToken = default)
        => await context.EvidencePacks
            .SingleOrDefaultAsync(e => e.WorkflowInstanceId == instanceId, cancellationToken);

    /// <summary>Lista EvidencePacks associados a um conjunto de releases (batch lookup para relatórios de conformidade).</summary>
    public async Task<IReadOnlyList<EvidencePack>> ListByReleaseIdsAsync(IEnumerable<Guid> releaseIds, CancellationToken cancellationToken = default)
    {
        var ids = releaseIds.ToList();
        return await context.EvidencePacks
            .Where(e => ids.Contains(e.ReleaseId))
            .ToListAsync(cancellationToken);
    }
}
