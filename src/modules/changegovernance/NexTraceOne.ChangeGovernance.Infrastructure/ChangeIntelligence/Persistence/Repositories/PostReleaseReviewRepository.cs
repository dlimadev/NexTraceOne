using Microsoft.EntityFrameworkCore;
using NexTraceOne.ChangeIntelligence.Application.Abstractions;
using NexTraceOne.ChangeIntelligence.Domain.Entities;

namespace NexTraceOne.ChangeIntelligence.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de reviews automáticas pós-release.
/// </summary>
internal sealed class PostReleaseReviewRepository(ChangeIntelligenceDbContext context) : IPostReleaseReviewRepository
{
    /// <summary>Busca a review pós-release de uma release.</summary>
    public async Task<PostReleaseReview?> GetByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken = default)
        => await context.PostReleaseReviews
            .SingleOrDefaultAsync(r => r.ReleaseId == releaseId, cancellationToken);

    /// <summary>Adiciona uma review pós-release.</summary>
    public void Add(PostReleaseReview review)
        => context.PostReleaseReviews.Add(review);
}
