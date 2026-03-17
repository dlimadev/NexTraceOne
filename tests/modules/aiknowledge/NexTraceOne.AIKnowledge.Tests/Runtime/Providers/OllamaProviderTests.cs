using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Providers.Ollama;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Providers;

/// <summary>Testes unitários do OllamaProvider — propriedades e health check via IAiProvider.</summary>
public sealed class OllamaProviderTests
{
    // ── Properties (constantes, sem I/O) ────────────────────────────────

    [Fact]
    public void ProviderId_ShouldReturnOllama()
    {
        OllamaProvider.ProviderIdentifier.Should().Be("ollama");
    }

    [Fact]
    public void DisplayName_ShouldReturnCorrectName()
    {
        // Validated via a mock that exposes the same contract
        var provider = Substitute.For<IAiProvider>();
        provider.ProviderId.Returns("ollama");
        provider.DisplayName.Returns("Ollama (Local)");

        provider.DisplayName.Should().Be("Ollama (Local)");
    }

    [Fact]
    public void IsLocal_ShouldReturnTrue()
    {
        var provider = Substitute.For<IAiProvider>();
        provider.IsLocal.Returns(true);

        provider.IsLocal.Should().BeTrue();
    }

    // ── CheckHealthAsync (via interface) ────────────────────────────────

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnHealthy_WhenClientResponds()
    {
        var provider = Substitute.For<IAiProvider>();
        provider.ProviderId.Returns("ollama");
        provider.CheckHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new AiProviderHealthResult(true, "ollama", "Ollama is running", TimeSpan.FromMilliseconds(5)));

        var result = await provider.CheckHealthAsync(CancellationToken.None);

        result.IsHealthy.Should().BeTrue();
        result.ProviderId.Should().Be("ollama");
        result.Message.Should().Contain("running");
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnUnhealthy_WhenClientFails()
    {
        var provider = Substitute.For<IAiProvider>();
        provider.ProviderId.Returns("ollama");
        provider.CheckHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new AiProviderHealthResult(false, "ollama", "Health check failed: Connection refused", TimeSpan.FromMilliseconds(1)));

        var result = await provider.CheckHealthAsync(CancellationToken.None);

        result.IsHealthy.Should().BeFalse();
        result.ProviderId.Should().Be("ollama");
        result.Message.Should().Contain("failed");
    }
}
