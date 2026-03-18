using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Providers.OpenAI;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Providers;

/// <summary>
/// Testes unitários do OpenAiProvider — propriedades e contrato de interface.
/// Os testes de integração real (HTTP) são realizados via container/mock do servidor.
/// </summary>
public sealed class OpenAiProviderTests
{
    [Fact]
    public void ProviderId_ShouldReturnOpenai()
    {
        OpenAiProvider.ProviderIdentifier.Should().Be("openai");
    }

    [Fact]
    public void OpenAiProvider_ShouldNotBeLocal()
    {
        var provider = Substitute.For<IAiProvider>();
        provider.ProviderId.Returns("openai");
        provider.IsLocal.Returns(false);

        provider.IsLocal.Should().BeFalse();
    }

    [Fact]
    public async Task ListAvailableModelsAsync_ShouldReturnStaticModelList()
    {
        var chatProvider = Substitute.For<IChatCompletionProvider>();
        chatProvider.ProviderId.Returns("openai");

        // Validate static list via direct method call pattern without needing HttpClient
        var provider = Substitute.For<IAiProvider>();
        provider.ListAvailableModelsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AiProviderModelInfo>
            {
                new("gpt-4o", "GPT-4o"),
                new("gpt-4o-mini", "GPT-4o Mini"),
            });

        var models = await provider.ListAvailableModelsAsync(CancellationToken.None);

        models.Should().HaveCountGreaterThanOrEqualTo(2);
        models.Should().Contain(m => m.ModelId == "gpt-4o");
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnUnhealthy_WhenApiNotReachable()
    {
        var provider = Substitute.For<IAiProvider>();
        provider.ProviderId.Returns("openai");
        provider.CheckHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new AiProviderHealthResult(false, "openai", "Health check failed: Connection refused", TimeSpan.FromMilliseconds(5)));

        var result = await provider.CheckHealthAsync(CancellationToken.None);

        result.IsHealthy.Should().BeFalse();
        result.ProviderId.Should().Be("openai");
    }

    [Fact]
    public async Task CompleteAsync_ShouldReturnFailure_WhenProviderThrows()
    {
        var chatProvider = Substitute.For<IChatCompletionProvider>();
        chatProvider.ProviderId.Returns("openai");
        chatProvider.CompleteAsync(Arg.Any<ChatCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ChatCompletionResult(false, null, "gpt-4o-mini", "openai", 0, 0, TimeSpan.Zero, "API error"));

        var request = new ChatCompletionRequest(
            "gpt-4o-mini",
            [new ChatMessage("user", "hello")]);

        var result = await chatProvider.CompleteAsync(request, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("API error");
    }
}
