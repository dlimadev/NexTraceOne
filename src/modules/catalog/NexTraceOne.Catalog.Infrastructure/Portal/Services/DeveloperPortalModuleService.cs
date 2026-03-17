using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Contracts.Portal.ServiceInterfaces;

namespace NexTraceOne.Catalog.Infrastructure.Portal.Services;

/// <summary>
/// Implementação do contrato público do módulo DeveloperPortal.
/// Outros módulos consomem esta interface — nunca o DbContext ou repositórios diretamente.
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
