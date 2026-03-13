using NexTraceOne.DeveloperPortal.Domain.Entities;

namespace NexTraceOne.DeveloperPortal.Application.Abstractions;

/// <summary>
/// Repositório de subscrições de API do módulo DeveloperPortal.
/// Gerencia persistência de preferências de notificação dos consumidores.
/// </summary>
public interface ISubscriptionRepository
{
    Task<Subscription?> GetByIdAsync(SubscriptionId id, CancellationToken ct = default);
    Task<Subscription?> GetByApiAndSubscriberAsync(Guid apiAssetId, Guid subscriberId, CancellationToken ct = default);
    Task<IReadOnlyList<Subscription>> GetBySubscriberAsync(Guid subscriberId, CancellationToken ct = default);
    Task<IReadOnlyList<Subscription>> GetByApiAssetAsync(Guid apiAssetId, CancellationToken ct = default);
    void Add(Subscription subscription);
    void Update(Subscription subscription);
    void Remove(Subscription subscription);
}
