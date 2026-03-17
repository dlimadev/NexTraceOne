using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Services;

/// <summary>Testes unitários do AiProviderHealthService — health checks de providers.</summary>
public sealed class AiProviderHealthServiceTests
{
    private readonly IAiProviderFactory _factory = Substitute.For<IAiProviderFactory>();

    private IAiProvider CreateHealthyProvider(string id)
    {
        var provider = Substitute.For<IAiProvider>();
        provider.ProviderId.Returns(id);
        provider.CheckHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new AiProviderHealthResult(true, id, "OK", TimeSpan.FromMilliseconds(10)));
        return provider;
    }

    // ── CheckAllProvidersAsync ──────────────────────────────────────────

    [Fact]
    public async Task CheckAllProvidersAsync_ShouldReturnHealthForAllProviders()
    {
        var p1 = CreateHealthyProvider("ollama");
        var p2 = CreateHealthyProvider("openai");
        _factory.GetAllProviders().Returns([p1, p2]);

        var service = new AiProviderHealthService(_factory);
        var results = await service.CheckAllProvidersAsync(CancellationToken.None);

        results.Should().HaveCount(2);
        results.Should().AllSatisfy(r => r.IsHealthy.Should().BeTrue());
    }

    // ── CheckProviderAsync ──────────────────────────────────────────────

    [Fact]
    public async Task CheckProviderAsync_ShouldReturnNotFoundForUnknownProvider()
    {
        _factory.GetProvider("unknown").Returns((IAiProvider?)null);

        var service = new AiProviderHealthService(_factory);
        var result = await service.CheckProviderAsync("unknown", CancellationToken.None);

        result.IsHealthy.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task CheckProviderAsync_ShouldReturnHealthForKnownProvider()
    {
        var provider = CreateHealthyProvider("ollama");
        _factory.GetProvider("ollama").Returns(provider);

        var service = new AiProviderHealthService(_factory);
        var result = await service.CheckProviderAsync("ollama", CancellationToken.None);

        result.IsHealthy.Should().BeTrue();
        result.ProviderId.Should().Be("ollama");
    }
}
