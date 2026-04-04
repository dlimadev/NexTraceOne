using Microsoft.EntityFrameworkCore;

using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de Post-Incident Reviews (PIR).
/// Persiste e consulta entidades PostIncidentReview no IncidentDbContext.
/// </summary>
internal sealed class EfPostIncidentReviewRepository(IncidentDbContext context) : IPostIncidentReviewRepository
{
    /// <inheritdoc />
    public async Task<PostIncidentReview?> GetByIncidentIdAsync(Guid incidentId, CancellationToken cancellationToken)
        => await context.PostIncidentReviews
            .SingleOrDefaultAsync(r => r.IncidentId == incidentId, cancellationToken);

    /// <inheritdoc />
    public async Task<PostIncidentReview?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await context.PostIncidentReviews
            .SingleOrDefaultAsync(r => r.Id == new PostIncidentReviewId(id), cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(PostIncidentReview review, CancellationToken cancellationToken)
    {
        context.PostIncidentReviews.Add(review);
        await context.CommitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(PostIncidentReview review, CancellationToken cancellationToken)
    {
        context.PostIncidentReviews.Update(review);
        await context.CommitAsync(cancellationToken);
    }
}
