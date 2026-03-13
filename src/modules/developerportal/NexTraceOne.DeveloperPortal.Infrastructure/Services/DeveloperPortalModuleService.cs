using NexTraceOne.DeveloperPortal.Application.Abstractions;
using NexTraceOne.DeveloperPortal.Contracts.ServiceInterfaces;

namespace NexTraceOne.DeveloperPortal.Infrastructure.Services;

/// <summary>
/// Implementação do contrato público do módulo DeveloperPortal.
/// Outros módulos consomem esta interface — nunca o DbContext ou repositórios directamente.
/// Fornece queries de leitura sobre subscrições do portal para suporte a notificações
/// e exibição de popularidade de APIs no catálogo.
/// </summary>
internal sealed class DeveloperPortalModuleService(
    ISubscriptionRepository subscriptionRepository) : IDeveloperPortalModule
{
    /// <inheritdoc />
    public async Task<bool> HasActiveSubscriptionsAsync(
        Guid apiAssetId, CancellationToken cancellationToken)
    {
        var subscriptions = await subscriptionRepository
            .GetByApiAssetAsync(apiAssetId, cancellationToken);

        return subscriptions.Any(s => s.IsActive);
    }

    /// <inheritdoc />
    public async Task<int> GetActiveSubscriptionCountAsync(
        Guid apiAssetId, CancellationToken cancellationToken)
    {
        var subscriptions = await subscriptionRepository
            .GetByApiAssetAsync(apiAssetId, cancellationToken);

        return subscriptions.Count(s => s.IsActive);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetSubscriberIdsAsync(
        Guid apiAssetId, CancellationToken cancellationToken)
    {
        var subscriptions = await subscriptionRepository
            .GetByApiAssetAsync(apiAssetId, cancellationToken);

        return subscriptions
            .Where(s => s.IsActive)
            .Select(s => s.SubscriberId)
            .Distinct()
            .ToList()
            .AsReadOnly();
    }
}
