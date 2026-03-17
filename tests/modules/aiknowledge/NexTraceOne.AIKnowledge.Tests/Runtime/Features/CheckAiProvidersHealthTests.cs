using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Features.CheckAiProvidersHealth;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Features;

/// <summary>Testes unitários do handler CheckAiProvidersHealth — verificação de saúde dos providers.</summary>
public sealed class CheckAiProvidersHealthTests
{
    private readonly IAiProviderHealthService _healthService = Substitute.For<IAiProviderHealthService>();

    private CheckAiProvidersHealth.Handler CreateHandler() => new(_healthService);

    // ── Handle ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnHealthResultsForAllProviders()
    {
        _healthService.CheckAllProvidersAsync(Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new AiProviderHealthResult(true, "ollama", "OK", TimeSpan.FromMilliseconds(5)),
                new AiProviderHealthResult(true, "openai", "OK", TimeSpan.FromMilliseconds(50))
            });

        var handler = CreateHandler();
        var result = await handler.Handle(new CheckAiProvidersHealth.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task AllHealthy_ShouldBeTrue_WhenAllProvidersHealthy()
    {
        _healthService.CheckAllProvidersAsync(Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new AiProviderHealthResult(true, "ollama", "OK"),
                new AiProviderHealthResult(true, "openai", "OK")
            });

        var handler = CreateHandler();
        var result = await handler.Handle(new CheckAiProvidersHealth.Query(), CancellationToken.None);

        result.Value.AllHealthy.Should().BeTrue();
    }

    [Fact]
    public async Task AllHealthy_ShouldBeFalse_WhenAnyProviderUnhealthy()
    {
        _healthService.CheckAllProvidersAsync(Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new AiProviderHealthResult(true, "ollama", "OK"),
                new AiProviderHealthResult(false, "openai", "Timeout")
            });

        var handler = CreateHandler();
        var result = await handler.Handle(new CheckAiProvidersHealth.Query(), CancellationToken.None);

        result.Value.AllHealthy.Should().BeFalse();
    }
}
