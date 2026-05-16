using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class AiExternalInferenceRecordRepository(AiGovernanceDbContext context, ICurrentTenant currentTenant) : IAiExternalInferenceRecordRepository
{
    public async Task AddAsync(AiExternalInferenceRecord entity, CancellationToken ct)
        => await context.ExternalInferenceRecords.AddAsync(entity, ct);

    public async Task<AiExternalInferenceRecord?> GetByIdAsync(AiExternalInferenceRecordId id, CancellationToken ct)
        => await context.ExternalInferenceRecords.Where(e => e.TenantId == currentTenant.Id).SingleOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<AiExternalInferenceRecord>> GetAllAsync(CancellationToken ct)
        => await context.ExternalInferenceRecords.OrderByDescending(r => r.Id).ToListAsync(ct);

    public async Task<IReadOnlyList<AiExternalInferenceRecord>> GetPendingReviewAsync(CancellationToken ct)
        => await context.ExternalInferenceRecords
            .Where(r => r.PromotionStatus == AiKnowledgePromotionStatus.Pending)
            .OrderBy(r => r.Id)
            .ToListAsync(ct);
}
