using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de preferências de IA por usuário.
/// </summary>
public interface IUserAiPreferenceRepository
{
    Task<UserAiPreference?> GetByIdAsync(UserAiPreferenceId id, CancellationToken ct = default);

    Task<UserAiPreference?> GetByUserAndFeatureAsync(
        Guid userId,
        Guid tenantId,
        string featureKey,
        CancellationToken ct = default);

    Task<IReadOnlyList<UserAiPreference>> ListByUserAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken ct = default);

    Task<IReadOnlyList<UserAiPreference>> ListGlobalPreferencesAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken ct = default);

    Task<bool> ExistsAsync(
        Guid userId,
        Guid tenantId,
        string featureKey,
        CancellationToken ct = default);

    Task AddAsync(UserAiPreference preference, CancellationToken ct = default);

    Task UpdateAsync(UserAiPreference preference, CancellationToken ct = default);
}
