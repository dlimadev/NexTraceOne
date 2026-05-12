using Microsoft.Extensions.Configuration;
using NexTraceOne.BuildingBlocks.Observability;
using NexTraceOne.BuildingBlocks.Observability.Observability;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Tests.Observability;

public sealed class ObservabilityModeTests
{
    private static IConfiguration BuildConfig(string mode)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Platform:ObservabilityMode"] = mode,
            })
            .Build();

    [Fact]
    public void ResolveObservabilityMode_Full_ReturnsFull()
    {
        var config = BuildConfig("Full");
        var result = DependencyInjection.ResolveObservabilityMode(config);
        result.Should().Be(ObservabilityMode.Full);
    }

    [Fact]
    public void ResolveObservabilityMode_Lite_ReturnsLite()
    {
        var config = BuildConfig("Lite");
        var result = DependencyInjection.ResolveObservabilityMode(config);
        result.Should().Be(ObservabilityMode.Lite);
    }

    [Fact]
    public void ResolveObservabilityMode_Minimal_ReturnsMinimal()
    {
        var config = BuildConfig("Minimal");
        var result = DependencyInjection.ResolveObservabilityMode(config);
        result.Should().Be(ObservabilityMode.Minimal);
    }

    [Fact]
    public void ResolveObservabilityMode_Unknown_DefaultsFull()
    {
        var config = BuildConfig("UnknownValue");
        var result = DependencyInjection.ResolveObservabilityMode(config);
        result.Should().Be(ObservabilityMode.Full);
    }

    [Fact]
    public void ResolveObservabilityMode_CaseInsensitive_Works()
    {
        var config = BuildConfig("lite");
        var result = DependencyInjection.ResolveObservabilityMode(config);
        result.Should().Be(ObservabilityMode.Lite);
    }

    [Fact]
    public void ResolveObservabilityMode_Missing_DefaultsFull()
    {
        var config = new ConfigurationBuilder().Build();
        var result = DependencyInjection.ResolveObservabilityMode(config);
        result.Should().Be(ObservabilityMode.Full);
    }

    [Fact]
    public void ObservabilityMode_HasThreeValues()
    {
        var values = Enum.GetValues<ObservabilityMode>();
        values.Should().HaveCount(3);
        values.Should().Contain(ObservabilityMode.Full);
        values.Should().Contain(ObservabilityMode.Lite);
        values.Should().Contain(ObservabilityMode.Minimal);
    }
}
