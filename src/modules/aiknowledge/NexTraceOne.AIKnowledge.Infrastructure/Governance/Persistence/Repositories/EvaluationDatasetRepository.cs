using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class EvaluationDatasetRepository(AiGovernanceDbContext context, ICurrentTenant currentTenant) : IEvaluationDatasetRepository
{
    public void Add(EvaluationDataset dataset)
        => context.EvaluationDatasets.Add(dataset);

    public async Task<EvaluationDataset?> GetByIdAsync(EvaluationDatasetId id, CancellationToken ct)
        => await context.EvaluationDatasets.Where(e => e.TenantId == currentTenant.Id).SingleOrDefaultAsync(d => d.Id == id, ct);

    public async Task<IReadOnlyList<EvaluationDataset>> ListByTenantAsync(Guid tenantId, CancellationToken ct)
        => await context.EvaluationDatasets
            .Where(d => d.TenantId == tenantId)
            .OrderBy(d => d.Name)
            .ToListAsync(ct);
}
