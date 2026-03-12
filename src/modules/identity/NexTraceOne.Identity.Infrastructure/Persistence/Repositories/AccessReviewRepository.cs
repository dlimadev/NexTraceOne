using Microsoft.EntityFrameworkCore;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Infrastructure.Persistence;

namespace NexTraceOne.Identity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de campanhas de access review.
/// </summary>
internal sealed class AccessReviewRepository(IdentityDbContext dbContext) : IAccessReviewRepository
{
    public async Task<AccessReviewCampaign?> GetByIdWithItemsAsync(AccessReviewCampaignId id, CancellationToken cancellationToken)
        => await dbContext.AccessReviewCampaigns
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AccessReviewCampaign>> ListOpenByTenantAsync(TenantId tenantId, CancellationToken cancellationToken)
        => await dbContext.AccessReviewCampaigns
            .Where(x => x.TenantId == tenantId && x.Status == AccessReviewCampaignStatus.Open)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<AccessReviewItem?> GetItemByIdAsync(AccessReviewItemId id, CancellationToken cancellationToken)
        => await dbContext.AccessReviewItems
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AccessReviewItem>> ListPendingByReviewerAsync(UserId reviewerId, CancellationToken cancellationToken)
        => await dbContext.AccessReviewItems
            .Where(x => x.ReviewerId == reviewerId && x.Decision == AccessReviewDecision.Pending)
            .ToListAsync(cancellationToken);

    public void Add(AccessReviewCampaign campaign)
        => dbContext.AccessReviewCampaigns.Add(campaign);
}
