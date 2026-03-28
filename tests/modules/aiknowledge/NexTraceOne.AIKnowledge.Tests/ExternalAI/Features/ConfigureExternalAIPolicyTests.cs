using NexTraceOne.AIKnowledge.Application.ExternalAI.Features.ConfigureExternalAIPolicy;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Tests.ExternalAI.Features;

public sealed class ConfigureExternalAIPolicyTests
{
    private readonly IAiProviderFactory _providerFactory = Substitute.For<IAiProviderFactory>();

    [Fact]
    public async Task Handle_ShouldCatchMissingRequiredFields()
    {
        var handler = new ConfigureExternalAIPolicy.Handler(_providerFactory);
        var command = new ConfigureExternalAIPolicy.Command(
            ProviderId: "openai",
            ProviderType: "openai",
            EndpointUrl: "not-a-url",
            ApiKey: null,
            ModelName: "gpt 4o",
            TestConnectivity: false);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsValid.Should().BeFalse();
        result.Value.Errors.Should().Contain(x => x.Field == "endpointUrl");
        result.Value.Errors.Should().Contain(x => x.Field == "apiKey");
        result.Value.Errors.Should().Contain(x => x.Field == "modelName");
    }

    [Fact]
    public async Task Handle_ShouldPassForValidConfiguration()
    {
        var handler = new ConfigureExternalAIPolicy.Handler(_providerFactory);
        var command = new ConfigureExternalAIPolicy.Command(
            ProviderId: "openai",
            ProviderType: "openai",
            EndpointUrl: "https://api.openai.com",
            ApiKey: "sk-1234567890abcdef",
            ModelName: "gpt-4o",
            TestConnectivity: false);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsValid.Should().BeTrue();
        result.Value.Errors.Should().BeEmpty();
    }
}
