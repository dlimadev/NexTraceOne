using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

/// <summary>
/// Serviço de monitoramento de saúde dos providers. Executa health checks em paralelo.
/// </summary>
public sealed class AiProviderHealthService : IAiProviderHealthService
{
    private readonly IAiProviderFactory _providerFactory;

    public AiProviderHealthService(IAiProviderFactory providerFactory)
    {
        _providerFactory = providerFactory;
    }

    public async Task<IReadOnlyList<AiProviderHealthResult>> CheckAllProvidersAsync(
        CancellationToken cancellationToken = default)
    {
        var providers = _providerFactory.GetAllProviders();
        var tasks = providers.Select(p => p.CheckHealthAsync(cancellationToken));
        var results = await Task.WhenAll(tasks);
        return results;
    }

    public async Task<AiProviderHealthResult> CheckProviderAsync(
        string providerId,
        CancellationToken cancellationToken = default)
    {
        var provider = _providerFactory.GetProvider(providerId);
        if (provider is null)
        {
            return new AiProviderHealthResult(false, providerId, "Provider not found");
        }

        return await provider.CheckHealthAsync(cancellationToken);
    }
}
