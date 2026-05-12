using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.GetObservabilityMode;

/// <summary>
/// W7-03: Testes unitários do GetObservabilityMode.
/// Verifica leitura e atualização do modo de observabilidade (Full/Lite/Minimal).
/// </summary>
public sealed class GetObservabilityModeTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 5, 12, 14, 30, 0, TimeSpan.Zero);

    private static IDateTimeProvider CreateClock()
    {
        var clock = NSubstitute.Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    // ── Query Handler Tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenModeIsFull_ReturnsFullConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Platform:Observability:Mode"] = "Full",
                ["Elasticsearch:Url"] = "http://elasticsearch:9200",
                ["Platform:Observability:PostgresAnalyticsEnabled"] = "true",
                ["Platform:Observability:OtelCollectorEndpoint"] = "http://otel-collector:4317"
            })
            .Build();

        var handler = new GetObservabilityMode.Handler(config, CreateClock());
        var result = await handler.Handle(new GetObservabilityMode.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentMode.Should().Be("Full");
        result.Value.ElasticsearchConnected.Should().BeTrue();
        result.Value.Version.Should().Be("8.x");
        result.Value.PostgresAnalyticsEnabled.Should().BeTrue();
        result.Value.OtelCollectorConnected.Should().BeTrue();
        result.Value.AdditionalRamUsageGb.Should().Be(4.0);
        result.Value.TradeOffs.Should().Contain("Higher RAM usage");
        result.Value.TradeOffs.Should().Contain("Full trace correlation");
        result.Value.UpdatedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task Handle_WhenModeIsLite_ReturnsLiteConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Platform:Observability:Mode"] = "Lite",
                ["Elasticsearch:Url"] = "",
                ["Platform:Observability:PostgresAnalyticsEnabled"] = "true",
                ["Platform:Observability:OtelCollectorEndpoint"] = ""
            })
            .Build();

        var handler = new GetObservabilityMode.Handler(config, CreateClock());
        var result = await handler.Handle(new GetObservabilityMode.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentMode.Should().Be("Lite");
        result.Value.ElasticsearchConnected.Should().BeFalse();
        result.Value.Version.Should().BeNull();
        result.Value.PostgresAnalyticsEnabled.Should().BeTrue();
        result.Value.OtelCollectorConnected.Should().BeFalse();
        result.Value.AdditionalRamUsageGb.Should().Be(1.5);
        result.Value.TradeOffs.Should().Contain("Moderate RAM");
        result.Value.TradeOffs.Should().Contain("Limited trace sampling");
    }

    [Fact]
    public async Task Handle_WhenModeIsMinimal_ReturnsMinimalConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Platform:Observability:Mode"] = "Minimal",
                ["Elasticsearch:Url"] = null,
                ["Platform:Observability:PostgresAnalyticsEnabled"] = "false",
                ["Platform:Observability:OtelCollectorEndpoint"] = null
            })
            .Build();

        var handler = new GetObservabilityMode.Handler(config, CreateClock());
        var result = await handler.Handle(new GetObservabilityMode.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentMode.Should().Be("Minimal");
        result.Value.ElasticsearchConnected.Should().BeFalse();
        result.Value.PostgresAnalyticsEnabled.Should().BeFalse();
        result.Value.OtelCollectorConnected.Should().BeFalse();
        result.Value.AdditionalRamUsageGb.Should().Be(0.5);
        result.Value.TradeOffs.Should().Contain("Minimal RAM");
        result.Value.TradeOffs.Should().Contain("No traces");
    }

    [Fact]
    public async Task Handle_WhenModeNotConfigured_DefaultsToLite()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var handler = new GetObservabilityMode.Handler(config, CreateClock());
        var result = await handler.Handle(new GetObservabilityMode.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentMode.Should().Be("Lite");
    }

    [Fact]
    public async Task Handle_WhenElasticsearchUriProvided_DetectsConnection()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Platform:Observability:Mode"] = "Full",
                ["Elasticsearch:Uri"] = "http://localhost:9200"
            })
            .Build();

        var handler = new GetObservabilityMode.Handler(config, CreateClock());
        var result = await handler.Handle(new GetObservabilityMode.Query(), CancellationToken.None);

        result.Value.ElasticsearchConnected.Should().BeTrue();
        result.Value.Version.Should().Be("8.x");
    }

    // ── Update Command Handler Tests ────────────────────────────────────────────

    [Fact]
    public async Task UpdateHandler_WhenUpdatingToFull_ReturnsCorrectTradeOffs()
    {
        var handler = new GetObservabilityMode.UpdateHandler(CreateClock());
        var command = new GetObservabilityMode.UpdateObservabilityMode("Full");
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentMode.Should().Be("Full");
        result.Value.AdditionalRamUsageGb.Should().Be(4.0);
        result.Value.TradeOffs.Should().Contain("Higher RAM usage");
        result.Value.TradeOffs.Should().Contain("Full trace correlation");
        result.Value.TradeOffs.Should().Contain("Elasticsearch required");
        result.Value.UpdatedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task UpdateHandler_WhenUpdatingToLite_ReturnsCorrectTradeOffs()
    {
        var handler = new GetObservabilityMode.UpdateHandler(CreateClock());
        var command = new GetObservabilityMode.UpdateObservabilityMode("Lite");
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentMode.Should().Be("Lite");
        result.Value.AdditionalRamUsageGb.Should().Be(1.5);
        result.Value.TradeOffs.Should().Contain("Moderate RAM");
        result.Value.TradeOffs.Should().Contain("Limited trace sampling");
        result.Value.TradeOffs.Should().Contain("PostgreSQL analytics");
    }

    [Fact]
    public async Task UpdateHandler_WhenUpdatingToMinimal_ReturnsCorrectTradeOffs()
    {
        var handler = new GetObservabilityMode.UpdateHandler(CreateClock());
        var command = new GetObservabilityMode.UpdateObservabilityMode("Minimal");
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentMode.Should().Be("Minimal");
        result.Value.AdditionalRamUsageGb.Should().Be(0.5);
        result.Value.TradeOffs.Should().Contain("Minimal RAM");
        result.Value.TradeOffs.Should().Contain("No traces");
        result.Value.TradeOffs.Should().Contain("Metrics only");
    }
}
