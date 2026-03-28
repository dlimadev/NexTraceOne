using System.Linq;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.AIKnowledge.Application.ExternalAI.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Entities;
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

    private static IExternalAiPolicyRepository CreatePolicyRepository(params ExternalAiPolicy[] policies)
    {
        var repo = Substitute.For<IExternalAiPolicyRepository>();
        var list = (IReadOnlyList<ExternalAiPolicy>)policies.ToList();
        repo.ListActiveAsync(Arg.Any<CancellationToken>()).Returns(list);
        return repo;
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RouteQueryAsync_ShouldReturnProviderResponse_WhenModelInRegistry()
    {
        var ollamaProvider = CreateChatProvider("ollama", "Hello from Ollama");
        var resolvedModel = new ResolvedModel(Guid.NewGuid(), "deepseek-r1:1.5b", "DeepSeek", "ollama", "ollama", true, "chat");

        var adapter = new ExternalAiRoutingPortAdapter(
            CreateFactory(ollamaProvider),
            CreateCatalog(resolvedModel),
            CreatePolicyRepository(),
            CreateOptions(),
            CreateLogger());

        var result = await adapter.RouteQueryAsync("context", "test query");

        result.Should().Be("Hello from Ollama");
    }

    [Fact]
    public async Task RouteQueryAsync_ShouldUseFallbackToConfiguredProvider_WhenModelRegistryEmpty()
    {
        var ollamaProvider = CreateChatProvider("ollama", "Config-based response");

        var adapter = new ExternalAiRoutingPortAdapter(
            CreateFactory(ollamaProvider),
            CreateCatalog(null),             // empty registry
            CreatePolicyRepository(),
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
            CreatePolicyRepository(),
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
            CreatePolicyRepository(),
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
            CreatePolicyRepository(),
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
            CreatePolicyRepository(),
            CreateOptions(preferredProvider: "openai", preferredModel: "gpt-4o-mini"),
            CreateLogger());

        // No preferred provider override in call — should use options.PreferredProvider
        var result = await adapter.RouteQueryAsync("ctx", "question");

        result.Should().Be("OpenAI response");
    }

    [Fact]
    public async Task RouteQueryAsync_ShouldReturnPolicyFallback_WhenCapabilityRequiresApproval()
    {
        var ollamaProvider = CreateChatProvider("ollama", "Should not reach this");
        var policy = ExternalAiPolicy.Create(
            "approval-policy", "desc", 100, 1000, requiresApproval: true,
            "ChangeAnalysis,ErrorDiagnosis", DateTimeOffset.UtcNow);

        var adapter = new ExternalAiRoutingPortAdapter(
            CreateFactory(ollamaProvider),
            CreateCatalog(null),
            CreatePolicyRepository(policy),
            CreateOptions("ollama", "deepseek-r1:1.5b", fallbackEnabled: true),
            CreateLogger());

        var result = await adapter.RouteQueryAsync(
            "ctx", "analyse this change",
            capability: "ChangeAnalysis",
            environment: null);

        result.Should().StartWith("[FALLBACK_PROVIDER_UNAVAILABLE]");
        result.Should().Contain("ChangeAnalysis");
        result.Should().Contain("approval");
    }

    [Fact]
    public async Task RouteQueryAsync_ShouldReturnPolicyFallback_WhenCapabilityInProductionAndPolicyCoverIt()
    {
        var ollamaProvider = CreateChatProvider("ollama", "Should not reach this");
        // Policy does NOT require approval but covers the capability
        var policy = ExternalAiPolicy.Create(
            "production-data-guard", "desc", 100, 1000, requiresApproval: false,
            "IncidentAnalysis", DateTimeOffset.UtcNow);

        var adapter = new ExternalAiRoutingPortAdapter(
            CreateFactory(ollamaProvider),
            CreateCatalog(null),
            CreatePolicyRepository(policy),
            CreateOptions("ollama", "deepseek-r1:1.5b", fallbackEnabled: true),
            CreateLogger());

        var result = await adapter.RouteQueryAsync(
            "ctx", "diagnose incident",
            capability: "IncidentAnalysis",
            environment: "production");

        result.Should().StartWith("[FALLBACK_PROVIDER_UNAVAILABLE]");
        result.Should().Contain("production environment");
    }

    [Fact]
    public async Task RouteQueryAsync_ShouldProceedNormally_WhenCapabilityNotCoveredByAnyPolicy()
    {
        var ollamaProvider = CreateChatProvider("ollama", "Normal response");
        // Policy covers a different capability
        var policy = ExternalAiPolicy.Create(
            "limited-policy", "desc", 100, 1000, requiresApproval: true,
            "ChangeAnalysis", DateTimeOffset.UtcNow);

        var adapter = new ExternalAiRoutingPortAdapter(
            CreateFactory(ollamaProvider),
            CreateCatalog(null),
            CreatePolicyRepository(policy),
            CreateOptions("ollama", "deepseek-r1:1.5b"),
            CreateLogger());

        var result = await adapter.RouteQueryAsync(
            "ctx", "query about contracts",
            capability: "ContractLookup",
            environment: "production");

        result.Should().Be("Normal response");
    }
}
