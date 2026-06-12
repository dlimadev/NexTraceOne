using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Infrastructure.Persistence;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class UserAiPreferenceRepository(AiHubDbContext context)
    : IUserAiPreferenceRepository
{
    public async Task<UserAiPreference?> GetByIdAsync(
        UserAiPreferenceId id,
        CancellationToken ct = default)
        => await context.UserAiPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<UserAiPreference?> GetByUserAndFeatureAsync(
        Guid userId,
        Guid tenantId,
        string featureKey,
        CancellationToken ct = default)
        => await context.UserAiPreferences
            .FirstOrDefaultAsync(
                p => p.UserId == userId
                     && p.TenantId == tenantId
                     && p.FeatureKey == featureKey
                     && p.IsActive,
                ct);

    public async Task<IReadOnlyList<UserAiPreference>> ListByUserAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken ct = default)
        => await context.UserAiPreferences
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.TenantId == tenantId && p.IsActive)
            .OrderBy(p => p.FeatureKey)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<UserAiPreference>> ListGlobalPreferencesAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken ct = default)
        => await context.UserAiPreferences
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.TenantId == tenantId && p.FeatureKey == "*" && p.IsActive)
            .ToListAsync(ct);

    public async Task<bool> ExistsAsync(
        Guid userId,
        Guid tenantId,
        string featureKey,
        CancellationToken ct = default)
        => await context.UserAiPreferences
            .AnyAsync(
                p => p.UserId == userId
                     && p.TenantId == tenantId
                     && p.FeatureKey == featureKey
                     && p.IsActive,
                ct);

    public async Task AddAsync(UserAiPreference preference, CancellationToken ct = default)
        => await context.UserAiPreferences.AddAsync(preference, ct);

    public Task UpdateAsync(UserAiPreference preference, CancellationToken ct = default)
    {
        context.UserAiPreferences.Update(preference);
        return Task.CompletedTask;
    }
}
