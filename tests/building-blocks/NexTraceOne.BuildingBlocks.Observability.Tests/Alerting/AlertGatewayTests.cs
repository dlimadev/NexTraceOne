using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NexTraceOne.BuildingBlocks.Observability.Alerting;
using NexTraceOne.BuildingBlocks.Observability.Alerting.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Alerting.Models;

namespace NexTraceOne.BuildingBlocks.Observability.Tests.Alerting;

/// <summary>
/// Testes do AlertGateway: dispatch para todos os canais, canal específico,
/// tratamento de falhas e cenários com canais vazios.
/// </summary>
public sealed class AlertGatewayTests
{
    private static AlertPayload CreateTestPayload() => new()
    {
        Title = "Test Alert",
        Description = "Something happened",
        Severity = AlertSeverity.Error,
        Source = "unit-tests"
    };

    private static IAlertChannel CreateMockChannel(string name, bool sendResult = true)
    {
        var channel = Substitute.For<IAlertChannel>();
        channel.ChannelName.Returns(name);
        channel.SendAsync(Arg.Any<AlertPayload>(), Arg.Any<CancellationToken>())
            .Returns(sendResult);
        return channel;
    }

    [Fact]
    public async Task DispatchAsync_AllChannels_DispatchesToAll()
    {
        var channel1 = CreateMockChannel("Webhook");
        var channel2 = CreateMockChannel("Email");
        var logger = Substitute.For<ILogger<AlertGateway>>();
        var gateway = new AlertGateway([channel1, channel2], logger);
        var payload = CreateTestPayload();

        var result = await gateway.DispatchAsync(payload);

        result.TotalChannels.Should().Be(2);
        result.AllSucceeded.Should().BeTrue();
        result.ChannelResults.Should().ContainKey("Webhook").WhoseValue.Should().BeTrue();
        result.ChannelResults.Should().ContainKey("Email").WhoseValue.Should().BeTrue();
        await channel1.Received(1).SendAsync(payload, Arg.Any<CancellationToken>());
        await channel2.Received(1).SendAsync(payload, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_SpecificChannel_DispatchesToNamedChannelOnly()
    {
        var webhook = CreateMockChannel("Webhook");
        var email = CreateMockChannel("Email");
        var logger = Substitute.For<ILogger<AlertGateway>>();
        var gateway = new AlertGateway([webhook, email], logger);
        var payload = CreateTestPayload();

        var result = await gateway.DispatchAsync(payload, "Webhook");

        result.TotalChannels.Should().Be(1);
        result.ChannelResults.Should().ContainKey("Webhook").WhoseValue.Should().BeTrue();
        await webhook.Received(1).SendAsync(payload, Arg.Any<CancellationToken>());
        await email.DidNotReceive().SendAsync(Arg.Any<AlertPayload>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_ChannelNotFound_ReturnsFalseForChannel()
    {
        var webhook = CreateMockChannel("Webhook");
        var logger = Substitute.For<ILogger<AlertGateway>>();
        var gateway = new AlertGateway([webhook], logger);
        var payload = CreateTestPayload();

        var result = await gateway.DispatchAsync(payload, "NonExistent");

        result.TotalChannels.Should().Be(1);
        result.ChannelResults.Should().ContainKey("NonExistent").WhoseValue.Should().BeFalse();
        result.AllSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task DispatchAsync_OneChannelFails_OthersSucceed()
    {
        var successChannel = CreateMockChannel("Webhook");
        var failChannel = Substitute.For<IAlertChannel>();
        failChannel.ChannelName.Returns("Email");
        failChannel.SendAsync(Arg.Any<AlertPayload>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("SMTP connection failed"));

        var logger = Substitute.For<ILogger<AlertGateway>>();
        var gateway = new AlertGateway([successChannel, failChannel], logger);
        var payload = CreateTestPayload();

        var result = await gateway.DispatchAsync(payload);

        result.TotalChannels.Should().Be(2);
        result.AllSucceeded.Should().BeFalse();
        result.AnySucceeded.Should().BeTrue();
        result.ChannelResults["Webhook"].Should().BeTrue();
        result.ChannelResults["Email"].Should().BeFalse();
        result.FailedChannels.Should().Be(1);
    }

    [Fact]
    public async Task DispatchAsync_EmptyChannels_ReturnsEmptyResult()
    {
        var logger = Substitute.For<ILogger<AlertGateway>>();
        var gateway = new AlertGateway([], logger);
        var payload = CreateTestPayload();

        var result = await gateway.DispatchAsync(payload);

        result.TotalChannels.Should().Be(0);
        result.AllSucceeded.Should().BeFalse();
        result.AnySucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task DispatchAsync_NullPayload_ThrowsArgumentNullException()
    {
        var logger = Substitute.For<ILogger<AlertGateway>>();
        var gateway = new AlertGateway([], logger);

        Func<Task> act = () => gateway.DispatchAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DispatchAsync_SpecificChannel_NullPayload_ThrowsArgumentNullException()
    {
        var logger = Substitute.For<ILogger<AlertGateway>>();
        var gateway = new AlertGateway([], logger);

        Func<Task> act = () => gateway.DispatchAsync(null!, "Webhook");

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DispatchAsync_ChannelReturnsFalse_IsReflectedInResult()
    {
        var failingChannel = CreateMockChannel("Webhook", sendResult: false);
        var logger = Substitute.For<ILogger<AlertGateway>>();
        var gateway = new AlertGateway([failingChannel], logger);
        var payload = CreateTestPayload();

        var result = await gateway.DispatchAsync(payload);

        result.TotalChannels.Should().Be(1);
        result.AllSucceeded.Should().BeFalse();
        result.ChannelResults["Webhook"].Should().BeFalse();
    }
}
