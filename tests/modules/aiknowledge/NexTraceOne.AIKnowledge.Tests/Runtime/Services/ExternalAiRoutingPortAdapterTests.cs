using System.Linq;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Configuration;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Services;

/// <summary>
/// Testes unitários do ExternalAiRoutingPortAdapter.
/// Valida: resolução de provider, fallback configurado, fallback determinístico quando provider indisponível.
/// </summary>
public sealed class ExternalAiRoutingPortAdapterTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static IChatCompletionProvider CreateChatProvider(
        string providerId,
        string? content = "Test response",
        bool success = true)
    {
        var provider = Substitute.For<IChatCompletionProvider>();
        provider.ProviderId.Returns(providerId);
        provider.CompleteAsync(Arg.Any<ChatCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ChatCompletionResult(success, content, "model", providerId, 10, 20, TimeSpan.FromMilliseconds(50)));
        return provider;
    }

    private static IAiProviderFactory CreateFactory(params IChatCompletionProvider[] chatProviders)
    {
        var factory = Substitute.For<IAiProviderFactory>();
        foreach (var p in chatProviders)
            factory.GetChatProvider(p.ProviderId).Returns(p);

        factory.GetAllProviders().Returns(chatProviders.OfType<IAiProvider>().ToList());
        return factory;
    }

    private static IAiModelCatalogService CreateCatalog(ResolvedModel? model = null)
    {
        var catalog = Substitute.For<IAiModelCatalogService>();
        catalog.ResolveDefaultModelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(model);
        return catalog;
    }

    private static IOptions<AiRoutingOptions> CreateOptions(
        string? preferredProvider = "ollama",
        string? preferredModel = "deepseek-r1:1.5b",
        bool fallbackEnabled = true)
    {
        var opts = new AiRoutingOptions
        {
            PreferredProvider = preferredProvider,
            PreferredChatModel = preferredModel,
            EnableDeterministicFallback = fallbackEnabled,
            FallbackPrefix = "[FALLBACK_PROVIDER_UNAVAILABLE]"
        };
        return Options.Create(opts);
    }

    private static ILogger<ExternalAiRoutingPortAdapter> CreateLogger()
        => Substitute.For<ILogger<ExternalAiRoutingPortAdapter>>();

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RouteQueryAsync_ShouldReturnProviderResponse_WhenModelInRegistry()
    {
        var ollamaProvider = CreateChatProvider("ollama", "Hello from Ollama");
        var resolvedModel = new ResolvedModel(Guid.NewGuid(), "deepseek-r1:1.5b", "DeepSeek", "ollama", "ollama", true, "chat");

        var adapter = new ExternalAiRoutingPortAdapter(
            CreateFactory(ollamaProvider),
            CreateCatalog(resolvedModel),
            CreateOptions(),
            CreateLogger());

        var result = await adapter.RouteQueryAsync("context", "what is this?");

        result.Should().Be("Hello from Ollama");
    }

    [Fact]
    public async Task RouteQueryAsync_ShouldUseFallbackToConfiguredProvider_WhenModelRegistryEmpty()
    {
        var ollamaProvider = CreateChatProvider("ollama", "Config-based response");

        var adapter = new ExternalAiRoutingPortAdapter(
            CreateFactory(ollamaProvider),
            CreateCatalog(null),             // empty registry
            CreateOptions("ollama", "deepseek-r1:1.5b"),
            CreateLogger());

        var result = await adapter.RouteQueryAsync("context", "test query");

        result.Should().Be("Config-based response");
    }

    [Fact]
    public async Task RouteQueryAsync_ShouldReturnDeterministicFallback_WhenProviderFails()
    {
        var failingProvider = CreateChatProvider("ollama", null, success: false);

        var adapter = new ExternalAiRoutingPortAdapter(
            CreateFactory(failingProvider),
            CreateCatalog(null),
            CreateOptions("ollama", "deepseek-r1:1.5b", fallbackEnabled: true),
            CreateLogger());

        var result = await adapter.RouteQueryAsync("ctx", "my question");

        result.Should().StartWith("[FALLBACK_PROVIDER_UNAVAILABLE]");
        result.Should().Contain("my question");
    }

    [Fact]
    public async Task RouteQueryAsync_ShouldReturnFallback_WhenNoProviderAvailableAndFallbackEnabled()
    {
        var emptyFactory = Substitute.For<IAiProviderFactory>();
        emptyFactory.GetChatProvider(Arg.Any<string>()).Returns((IChatCompletionProvider?)null);
        emptyFactory.GetAllProviders().Returns(new List<IAiProvider>());

        var adapter = new ExternalAiRoutingPortAdapter(
            emptyFactory,
            CreateCatalog(null),
            CreateOptions("ollama", "deepseek-r1:1.5b", fallbackEnabled: true),
            CreateLogger());

        var result = await adapter.RouteQueryAsync("ctx", "my question");

        result.Should().StartWith("[FALLBACK_PROVIDER_UNAVAILABLE]");
    }

    [Fact]
    public async Task RouteQueryAsync_ShouldThrow_WhenQueryIsEmpty()
    {
        var adapter = new ExternalAiRoutingPortAdapter(
            Substitute.For<IAiProviderFactory>(),
            CreateCatalog(null),
            CreateOptions(),
            CreateLogger());

        var act = async () => await adapter.RouteQueryAsync("ctx", "   ");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*AI query must not be empty*");
    }

    [Fact]
    public async Task RouteQueryAsync_ShouldPreferPreferredProvider_OverRegistryProvider()
    {
        var openAiProvider = CreateChatProvider("openai", "OpenAI response");
        var ollamaProvider = CreateChatProvider("ollama", "Ollama response");
        var resolvedModel = new ResolvedModel(Guid.NewGuid(), "deepseek-r1:1.5b", "DeepSeek", "ollama", "ollama", true, "chat");

        var factory = Substitute.For<IAiProviderFactory>();
        factory.GetChatProvider("openai").Returns(openAiProvider);
        factory.GetChatProvider("ollama").Returns(ollamaProvider);
        factory.GetAllProviders().Returns(new List<IAiProvider>());

        var adapter = new ExternalAiRoutingPortAdapter(
            factory,
            CreateCatalog(resolvedModel),
            CreateOptions(preferredProvider: "openai", preferredModel: "gpt-4o-mini"),
            CreateLogger());

        // No preferred provider override in call — should use options.PreferredProvider
        var result = await adapter.RouteQueryAsync("ctx", "question");

        result.Should().Be("OpenAI response");
    }
}
