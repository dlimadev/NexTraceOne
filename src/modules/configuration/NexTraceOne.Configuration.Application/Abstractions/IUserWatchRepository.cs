using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Abstractions;

/// <summary>Contrato do repositório de watch lists do utilizador.</summary>
public interface IUserWatchRepository
{
    Task<UserWatch?> GetByIdAsync(UserWatchId id, CancellationToken cancellationToken);
    Task<UserWatch?> GetByEntityAsync(string userId, string tenantId, string entityType, string entityId, CancellationToken cancellationToken);
    Task<IReadOnlyList<UserWatch>> ListByUserAsync(string userId, string tenantId, string? entityType, CancellationToken cancellationToken);
    Task AddAsync(UserWatch watch, CancellationToken cancellationToken);
    Task UpdateAsync(UserWatch watch, CancellationToken cancellationToken);
    Task DeleteAsync(UserWatch watch, CancellationToken cancellationToken);
}
