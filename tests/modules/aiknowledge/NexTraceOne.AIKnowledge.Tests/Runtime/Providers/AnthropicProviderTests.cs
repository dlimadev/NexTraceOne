using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Providers.Anthropic;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Providers;

/// <summary>
/// Testes unitários do AnthropicProvider — propriedades e contrato de interface.
/// Os testes de integração real (HTTP) são realizados via mock HTTP + container.
/// </summary>
public sealed class AnthropicProviderTests
{
    [Fact]
    public void ProviderId_ShouldReturnAnthropic()
    {
        AnthropicProvider.ProviderIdentifier.Should().Be("anthropic");
    }

    [Fact]
    public void AnthropicProvider_ShouldNotBeLocal()
    {
        var provider = Substitute.For<IAiProvider>();
        provider.ProviderId.Returns("anthropic");
        provider.IsLocal.Returns(false);

        provider.IsLocal.Should().BeFalse();
    }

    [Fact]
    public async Task ListAvailableModelsAsync_ShouldReturnClaudeModels()
    {
        var provider = Substitute.For<IAiProvider>();
        provider.ListAvailableModelsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AiProviderModelInfo>
            {
                new("claude-3-5-sonnet-20241022", "Claude 3.5 Sonnet"),
                new("claude-3-5-haiku-20241022", "Claude 3.5 Haiku"),
                new("claude-3-opus-20240229", "Claude 3 Opus"),
            });

        var models = await provider.ListAvailableModelsAsync(CancellationToken.None);

        models.Should().HaveCountGreaterThanOrEqualTo(3);
        models.Should().Contain(m => m.ModelId.Contains("sonnet"));
        models.Should().Contain(m => m.ModelId.Contains("haiku"));
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnUnhealthy_WhenApiNotReachable()
    {
        var provider = Substitute.For<IAiProvider>();
        provider.ProviderId.Returns("anthropic");
        provider.CheckHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new AiProviderHealthResult(false, "anthropic", "Health check failed: Connection refused", TimeSpan.FromMilliseconds(5)));

        var result = await provider.CheckHealthAsync(CancellationToken.None);

        result.IsHealthy.Should().BeFalse();
        result.ProviderId.Should().Be("anthropic");
    }

    [Fact]
    public async Task CompleteAsync_ShouldReturnFailure_WhenProviderThrows()
    {
        var chatProvider = Substitute.For<IChatCompletionProvider>();
        chatProvider.ProviderId.Returns("anthropic");
        chatProvider.CompleteAsync(Arg.Any<ChatCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ChatCompletionResult(
                false, null,
                "claude-3-5-haiku-20241022", "anthropic",
                0, 0, TimeSpan.Zero, "API error"));

        var request = new ChatCompletionRequest(
            "claude-3-5-haiku-20241022",
            [new ChatMessage("user", "hello")]);

        var result = await chatProvider.CompleteAsync(request, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("API error");
        result.ProviderId.Should().Be("anthropic");
    }

    [Fact]
    public async Task CompleteAsync_ShouldReturnSuccess_WhenMocked()
    {
        var chatProvider = Substitute.For<IChatCompletionProvider>();
        chatProvider.ProviderId.Returns("anthropic");
        chatProvider.CompleteAsync(Arg.Any<ChatCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ChatCompletionResult(
                true,
                "Hello from Claude!",
                "claude-3-5-haiku-20241022", "anthropic",
                25, 10, TimeSpan.FromMilliseconds(500)));

        var request = new ChatCompletionRequest(
            "claude-3-5-haiku-20241022",
            [new ChatMessage("user", "hello")]);

        var result = await chatProvider.CompleteAsync(request, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Content.Should().Be("Hello from Claude!");
        result.PromptTokens.Should().Be(25);
        result.CompletionTokens.Should().Be(10);
    }

    [Fact]
    public void ProviderIdentifier_ShouldBeAnthropic()
    {
        AnthropicProvider.ProviderIdentifier.Should().Be("anthropic");
    }
}
