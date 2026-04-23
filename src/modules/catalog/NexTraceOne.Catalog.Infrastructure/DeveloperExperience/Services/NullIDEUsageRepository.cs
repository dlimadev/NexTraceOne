using NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions;
using static NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions.IIDEUsageRepository;

namespace NexTraceOne.Catalog.Infrastructure.DeveloperExperience.Services;

/// <summary>
/// Implementação null (honest-null) de IIDEUsageRepository.
/// Descarta registos — sem base de dados configurada, uso IDE não é persistido.
/// Wave AK.1 — IDE Context API.
/// </summary>
public sealed class NullIDEUsageRepository : IIDEUsageRepository
{
    public Task AddAsync(IdeUsageRecord record, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<IdeUsageRecord>> ListByUserAsync(string userId, DateTimeOffset since, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<IdeUsageRecord>>([]);

    public Task<IReadOnlyList<IdeUsageRecord>> ListByTenantAsync(string tenantId, DateTimeOffset since, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<IdeUsageRecord>>([]);
}
