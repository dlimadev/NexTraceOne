using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Features.ListAiProviders;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Features;

/// <summary>Testes unitários do handler ListAiProviders — listagem de providers com status.</summary>
public sealed class ListAiProvidersTests
{
    private readonly IAiProviderFactory _factory = Substitute.For<IAiProviderFactory>();
    private readonly IAiProviderHealthService _healthService = Substitute.For<IAiProviderHealthService>();

    [Fact]
    public async Task Handle_ShouldReturnAllProvidersWithHealthStatus()
    {
        var provider = Substitute.For<IAiProvider>();
        provider.ProviderId.Returns("ollama");
        provider.DisplayName.Returns("Ollama (Local)");
        provider.IsLocal.Returns(true);

        _factory.GetAllProviders().Returns([provider]);
        _healthService.CheckAllProvidersAsync(Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new AiProviderHealthResult(true, "ollama", "OK", TimeSpan.FromMilliseconds(5))
            });

        var handler = new ListAiProviders.Handler(_factory, _healthService);
        var result = await handler.Handle(new ListAiProviders.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items[0].ProviderId.Should().Be("ollama");
        result.Value.Items[0].IsHealthy.Should().BeTrue();
        result.Value.Items[0].IsLocal.Should().BeTrue();
    }
}
