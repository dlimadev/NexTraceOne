using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Http;
using NSubstitute;
using Xunit;
using TestNetworkConnectivityFeature = NexTraceOne.Governance.Application.Features.TestNetworkConnectivity.TestNetworkConnectivity;

namespace NexTraceOne.Governance.Tests.Application.Features.TestNetworkConnectivity;

/// <summary>
/// Testes unitários para o TestNetworkConnectivity feature (W5-02).
/// Cobre: teste bem-sucedido, falha de conexão, timeout e URL customizada.
/// </summary>
public class TestNetworkConnectivityTests
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HttpClient _testClient;

    public TestNetworkConnectivityTests()
    {
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _testClient = new HttpClient();
        _httpClientFactory.CreateClient("network-test").Returns(_testClient);
    }

    [Fact]
    public async Task Handle_WhenTargetIsAccessible_ShouldReturnSuccessWithStatusCode()
    {
        // Arrange
        var handler = new TestNetworkConnectivityFeature.Handler(_httpClientFactory);
        var command = new TestNetworkConnectivityFeature.Command(null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.TestedUrl.Should().Be("https://www.example.com");
        result.Value.DurationMs.Should().BeGreaterThan(0);
        result.Value.HttpStatusCode.Should().NotBeNull();
        result.Value.Error.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenCustomUrlProvided_ShouldTestThatUrl()
    {
        // Arrange
        var handler = new TestNetworkConnectivityFeature.Handler(_httpClientFactory);
        var command = new TestNetworkConnectivityFeature.Command("https://httpbin.org/get");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TestedUrl.Should().Be("https://httpbin.org/get");
    }

    [Fact]
    public async Task Handle_WhenConnectionFails_ShouldReturnFailureWithError()
    {
        // Arrange
        var failingClient = new HttpClient(new FailingHttpMessageHandler());
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("network-test").Returns(failingClient);

        var handler = new TestNetworkConnectivityFeature.Handler(factory);
        var command = new TestNetworkConnectivityFeature.Command("https://invalid-host-that-does-not-exist.local");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeFalse();
        result.Value.Error.Should().NotBeNullOrEmpty();
        result.Value.HttpStatusCode.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenTimeoutOccurs_ShouldReturnFailureWithTimeoutError()
    {
        // Arrange
        var slowClient = new HttpClient(new SlowHttpMessageHandler())
        {
            Timeout = TimeSpan.FromMilliseconds(100)
        };
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("network-test").Returns(slowClient);

        var handler = new TestNetworkConnectivityFeature.Handler(factory);
        var command = new TestNetworkConnectivityFeature.Command("https://httpbin.org/delay/5");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeFalse();
        result.Value.Error.Should().ContainAny("TaskCanceled", "timeout", "cancel");
    }

    private sealed class FailingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Connection refused");
        }
    }

    private sealed class SlowHttpMessageHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
