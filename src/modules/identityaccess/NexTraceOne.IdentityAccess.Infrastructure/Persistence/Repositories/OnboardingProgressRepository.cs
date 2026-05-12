using Microsoft.EntityFrameworkCore;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Repositories;

/// <summary>Repositório EF Core de OnboardingProgress.</summary>
internal sealed class OnboardingProgressRepository(IdentityDbContext db) : IOnboardingProgressRepository
{
    /// <inheritdoc/>
    public async Task<OnboardingProgress?> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken)
        => await db.OnboardingProgresses
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(OnboardingProgress progress, CancellationToken cancellationToken)
        => await db.OnboardingProgresses.AddAsync(progress, cancellationToken);

    /// <inheritdoc/>
    public Task UpdateAsync(OnboardingProgress progress, CancellationToken cancellationToken)
    {
        db.OnboardingProgresses.Update(progress);
        return Task.CompletedTask;
    }
}
