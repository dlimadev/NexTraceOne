using NexTraceOne.AIKnowledge.Application.ExternalAI.Features.QueryExternalAISimple;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.ExternalAI.Features;

public sealed class QueryExternalAISimpleTests
{
    private readonly IAiProviderHealthService _healthService = Substitute.For<IAiProviderHealthService>();

    private QueryExternalAISimple.Handler CreateHandler() => new(_healthService);

    [Fact]
    public async Task Handle_ShouldReturnHealthy_WhenProviderReachable()
    {
        _healthService.CheckProviderAsync("ollama", Arg.Any<CancellationToken>())
            .Returns(new AiProviderHealthResult(true, "ollama", "OK", TimeSpan.FromMilliseconds(12)));

        var result = await CreateHandler().Handle(new QueryExternalAISimple.Command("ollama"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalProviders.Should().Be(1);
        result.Value.Providers[0].ProviderId.Should().Be("ollama");
        result.Value.Providers[0].Status.Should().Be("Healthy");
        result.Value.Providers[0].ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnUnhealthy_WhenProviderUnreachable()
    {
        _healthService.CheckProviderAsync("openai", Arg.Any<CancellationToken>())
            .Returns(new AiProviderHealthResult(false, "openai", "timeout", TimeSpan.FromMilliseconds(5020)));

        var result = await CreateHandler().Handle(new QueryExternalAISimple.Command("openai"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Providers.Should().ContainSingle();
        result.Value.Providers[0].Status.Should().Be("Unhealthy");
        result.Value.Providers[0].ErrorMessage.Should().Be("timeout");
    }

    [Fact]
    public async Task Handle_ShouldCheckAllProviders_WhenProviderIdNotProvided()
    {
        _healthService.CheckAllProvidersAsync(Arg.Any<CancellationToken>())
            .Returns(
            [
                new AiProviderHealthResult(true, "ollama", "OK", TimeSpan.FromMilliseconds(10)),
                new AiProviderHealthResult(false, "openai", "down", TimeSpan.FromMilliseconds(50))
            ]);

        var result = await CreateHandler().Handle(new QueryExternalAISimple.Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalProviders.Should().Be(2);
        result.Value.Providers.Should().Contain(x => x.ProviderId == "ollama" && x.Status == "Healthy");
        result.Value.Providers.Should().Contain(x => x.ProviderId == "openai" && x.Status == "Unhealthy");
    }
}
