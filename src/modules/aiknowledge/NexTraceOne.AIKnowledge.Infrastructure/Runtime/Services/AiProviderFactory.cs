using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

/// <summary>
/// Fábrica de providers de IA. Centraliza a resolução de providers registrados via DI.
/// Novos providers são adicionados registrando-os como IAiProvider no container.
/// </summary>
public sealed class AiProviderFactory : IAiProviderFactory
{
    private readonly IReadOnlyDictionary<string, IAiProvider> _providers;
    private readonly IReadOnlyDictionary<string, IChatCompletionProvider> _chatProviders;

    public AiProviderFactory(
        IEnumerable<IAiProvider> providers,
        IEnumerable<IChatCompletionProvider> chatProviders)
    {
        _providers = providers.ToDictionary(p => p.ProviderId, StringComparer.OrdinalIgnoreCase);
        _chatProviders = chatProviders.ToDictionary(p => p.ProviderId, StringComparer.OrdinalIgnoreCase);
    }

    public IAiProvider? GetProvider(string providerId)
        => _providers.GetValueOrDefault(providerId);

    public IChatCompletionProvider? GetChatProvider(string providerId)
        => _chatProviders.GetValueOrDefault(providerId);

    public IReadOnlyList<IAiProvider> GetAllProviders()
        => _providers.Values.ToList();
}
