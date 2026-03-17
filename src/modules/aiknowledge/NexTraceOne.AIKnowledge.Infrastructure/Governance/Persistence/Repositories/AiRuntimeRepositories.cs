using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class AiProviderRepository(AiGovernanceDbContext context) : IAiProviderRepository
{
    public async Task<AiProvider?> GetByIdAsync(AiProviderId id, CancellationToken ct)
        => await context.Providers.SingleOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<AiProvider>> GetAllAsync(CancellationToken ct)
        => await context.Providers.OrderBy(p => p.Priority).ToListAsync(ct);

    public async Task<IReadOnlyList<AiProvider>> GetEnabledAsync(CancellationToken ct)
        => await context.Providers
            .Where(p => p.IsEnabled)
            .OrderBy(p => p.Priority)
            .ToListAsync(ct);

    public async Task AddAsync(AiProvider entity, CancellationToken ct)
        => await context.Providers.AddAsync(entity, ct);
}

internal sealed class AiSourceRepository(AiGovernanceDbContext context) : IAiSourceRepository
{
    public async Task<AiSource?> GetByIdAsync(AiSourceId id, CancellationToken ct)
        => await context.Sources.SingleOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<AiSource>> GetAllAsync(CancellationToken ct)
        => await context.Sources.OrderBy(s => s.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<AiSource>> GetByTypeAsync(AiSourceType sourceType, CancellationToken ct)
        => await context.Sources
            .Where(s => s.SourceType == sourceType)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AiSource>> GetEnabledAsync(CancellationToken ct)
        => await context.Sources
            .Where(s => s.IsEnabled)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public async Task AddAsync(AiSource entity, CancellationToken ct)
        => await context.Sources.AddAsync(entity, ct);
}

internal sealed class AiTokenQuotaPolicyRepository(AiGovernanceDbContext context) : IAiTokenQuotaPolicyRepository
{
    public async Task<AiTokenQuotaPolicy?> GetByIdAsync(AiTokenQuotaPolicyId id, CancellationToken ct)
        => await context.TokenQuotaPolicies.SingleOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<AiTokenQuotaPolicy>> GetAllAsync(CancellationToken ct)
        => await context.TokenQuotaPolicies.OrderBy(p => p.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<AiTokenQuotaPolicy>> GetByScopeAsync(string scope, CancellationToken ct)
        => await context.TokenQuotaPolicies
            .Where(p => p.Scope == scope)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AiTokenQuotaPolicy>> GetForUserAsync(string userId, CancellationToken ct)
        => await context.TokenQuotaPolicies
            .Where(p => p.Scope == "user" && p.ScopeValue == userId && p.IsEnabled)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AiTokenQuotaPolicy>> GetForTenantAsync(string tenantId, CancellationToken ct)
        => await context.TokenQuotaPolicies
            .Where(p => p.Scope == "tenant" && p.ScopeValue == tenantId && p.IsEnabled)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

    public async Task AddAsync(AiTokenQuotaPolicy entity, CancellationToken ct)
        => await context.TokenQuotaPolicies.AddAsync(entity, ct);
}

internal sealed class AiTokenUsageLedgerRepository(AiGovernanceDbContext context) : IAiTokenUsageLedgerRepository
{
    public async Task AddAsync(AiTokenUsageLedger entity, CancellationToken ct)
        => await context.TokenUsageLedger.AddAsync(entity, ct);

    public async Task<IReadOnlyList<AiTokenUsageLedger>> GetByUserAsync(string userId, CancellationToken ct)
        => await context.TokenUsageLedger
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AiTokenUsageLedger>> GetByTenantAsync(string tenantId, CancellationToken ct)
        => await context.TokenUsageLedger
            .Where(e => e.TenantId == tenantId)
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync(ct);

    public async Task<long> GetTotalTokensForPeriodAsync(
        string userId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
        => await context.TokenUsageLedger
            .Where(e => e.UserId == userId && e.Timestamp >= start && e.Timestamp <= end && !e.IsBlocked)
            .SumAsync(e => (long)e.TotalTokens, ct);
}

internal sealed class AiExternalInferenceRecordRepository(AiGovernanceDbContext context) : IAiExternalInferenceRecordRepository
{
    public async Task AddAsync(AiExternalInferenceRecord entity, CancellationToken ct)
        => await context.ExternalInferenceRecords.AddAsync(entity, ct);

    public async Task<AiExternalInferenceRecord?> GetByIdAsync(AiExternalInferenceRecordId id, CancellationToken ct)
        => await context.ExternalInferenceRecords.SingleOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<AiExternalInferenceRecord>> GetAllAsync(CancellationToken ct)
        => await context.ExternalInferenceRecords.OrderByDescending(r => r.Id).ToListAsync(ct);

    public async Task<IReadOnlyList<AiExternalInferenceRecord>> GetPendingReviewAsync(CancellationToken ct)
        => await context.ExternalInferenceRecords
            .Where(r => r.PromotionStatus == AiKnowledgePromotionStatus.Pending)
            .OrderBy(r => r.Id)
            .ToListAsync(ct);
}
