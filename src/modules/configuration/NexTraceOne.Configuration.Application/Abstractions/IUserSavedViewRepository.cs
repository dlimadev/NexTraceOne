using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Abstractions;

/// <summary>Abstração do repositório de vistas guardadas por utilizador.</summary>
public interface IUserSavedViewRepository
{
    Task<UserSavedView?> GetByIdAsync(UserSavedViewId id, CancellationToken ct);
    Task<IReadOnlyList<UserSavedView>> ListByUserAsync(string userId, string? context, CancellationToken ct);
    Task<IReadOnlyList<UserSavedView>> ListSharedByContextAsync(string context, string tenantId, CancellationToken ct);
    Task AddAsync(UserSavedView view, CancellationToken ct);
    Task UpdateAsync(UserSavedView view, CancellationToken ct);
    Task DeleteAsync(UserSavedView view, CancellationToken ct);
}
