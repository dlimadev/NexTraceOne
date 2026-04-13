using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Services;

/// <summary>
/// Proxy que resolve IEmbeddingProvider de um scope dedicado por chamada.
/// Necessário para usar InMemoryEmbeddingCacheService (Singleton) com IEmbeddingProvider (Scoped)
/// sem captive dependency.
/// </summary>
internal sealed class ScopedEmbeddingProviderProxy(IServiceScopeFactory scopeFactory) : IEmbeddingProvider
{
    public string ProviderId => "scoped-proxy";

    public async Task<EmbeddingResult> GenerateEmbeddingsAsync(
        EmbeddingRequest request,
        CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IEmbeddingProvider>();
        return await provider.GenerateEmbeddingsAsync(request, cancellationToken);
    }
}
