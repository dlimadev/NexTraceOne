using System.Linq;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Services;

/// <summary>Testes unitários do AiProviderFactory — resolução de providers registrados.</summary>
public sealed class AiProviderFactoryTests
{
    private static IAiProvider CreateMockProvider(string id, string displayName = "Test", bool isLocal = true)
    {
        var provider = Substitute.For<IAiProvider>();
        provider.ProviderId.Returns(id);
        provider.DisplayName.Returns(displayName);
        provider.IsLocal.Returns(isLocal);
        return provider;
    }

    private static IChatCompletionProvider CreateMockChatProvider(string id)
    {
        var provider = Substitute.For<IChatCompletionProvider>();
        provider.ProviderId.Returns(id);
        return provider;
    }

    // ── GetProvider ──────────────────────────────────────────────────────

    [Fact]
    public void GetProvider_ShouldReturnCorrectProvider()
    {
        var ollama = CreateMockProvider("ollama");
        var openai = CreateMockProvider("openai");
        var factory = new AiProviderFactory([ollama, openai], []);

        var result = factory.GetProvider("ollama");

        result.Should().NotBeNull();
        result!.ProviderId.Should().Be("ollama");
    }

    [Fact]
    public void GetProvider_ShouldReturnNullForUnknownProvider()
    {
        var factory = new AiProviderFactory([CreateMockProvider("ollama")], []);

        var result = factory.GetProvider("unknown");

        result.Should().BeNull();
    }

    // ── GetChatProvider ─────────────────────────────────────────────────

    [Fact]
    public void GetChatProvider_ShouldReturnCorrectProvider()
    {
        var chat = CreateMockChatProvider("ollama");
        var factory = new AiProviderFactory([], [chat]);

        var result = factory.GetChatProvider("ollama");

        result.Should().NotBeNull();
        result!.ProviderId.Should().Be("ollama");
    }

    [Fact]
    public void GetChatProvider_ShouldReturnNullForUnknownProvider()
    {
        var factory = new AiProviderFactory([], [CreateMockChatProvider("ollama")]);

        var result = factory.GetChatProvider("unknown");

        result.Should().BeNull();
    }

    // ── GetAllProviders ─────────────────────────────────────────────────

    [Fact]
    public void GetAllProviders_ShouldReturnAllRegisteredProviders()
    {
        var providers = new[]
        {
            CreateMockProvider("ollama"),
            CreateMockProvider("openai"),
            CreateMockProvider("azure")
        };
        var factory = new AiProviderFactory(providers, []);

        var result = factory.GetAllProviders();

        result.Should().HaveCount(3);
        result.Select(p => p.ProviderId).Should().Contain(["ollama", "openai", "azure"]);
    }
}
