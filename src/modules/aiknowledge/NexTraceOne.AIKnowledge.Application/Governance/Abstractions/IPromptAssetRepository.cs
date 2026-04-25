using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

public interface IPromptAssetRepository
{
    Task<PromptAsset?> FindByIdAsync(PromptAssetId id, CancellationToken ct = default);
    Task<PromptAsset?> FindBySlugAsync(string slug, Guid? tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<PromptAsset>> ListAsync(Guid? tenantId, string? category, bool activeOnly, CancellationToken ct = default);
    Task AddAsync(PromptAsset asset, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, Guid? tenantId, CancellationToken ct = default);
}
