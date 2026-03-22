using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Observability.Alerting.Channels;
using NexTraceOne.BuildingBlocks.Observability.Alerting.Configuration;
using NexTraceOne.BuildingBlocks.Observability.Alerting.Models;

namespace NexTraceOne.BuildingBlocks.Observability.Tests.Alerting;

/// <summary>
/// Testes do WebhookAlertChannel: nome do canal, URL não configurado e serialização do payload.
/// </summary>
public sealed class WebhookAlertChannelTests
{
    private static AlertPayload CreateTestPayload() => new()
    {
        Title = "Webhook Test",
        Description = "Testing webhook dispatch",
        Severity = AlertSeverity.Warning,
        Source = "unit-tests"
    };

    private static WebhookAlertChannel CreateChannel(AlertingOptions? options = null)
    {
        var opts = Options.Create(options ?? new AlertingOptions());
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(WebhookAlertChannel.HttpClientName).Returns(new HttpClient());
        var logger = Substitute.For<ILogger<WebhookAlertChannel>>();
        return new WebhookAlertChannel(factory, opts, logger);
    }

    [Fact]
    public void ChannelName_IsWebhook()
    {
        var channel = CreateChannel();

        channel.ChannelName.Should().Be("Webhook");
    }

    [Fact]
    public async Task SendAsync_WhenUrlNotConfigured_ReturnsFalse()
    {
        var channel = CreateChannel(new AlertingOptions
        {
            Webhook = new WebhookChannelOptions { Url = null }
        });

        var result = await channel.SendAsync(CreateTestPayload());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_WhenUrlEmpty_ReturnsFalse()
    {
        var channel = CreateChannel(new AlertingOptions
        {
            Webhook = new WebhookChannelOptions { Url = "  " }
        });

        var result = await channel.SendAsync(CreateTestPayload());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_WithValidUrl_PostsPayload()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(WebhookAlertChannel.HttpClientName).Returns(httpClient);

        var options = Options.Create(new AlertingOptions
        {
            Webhook = new WebhookChannelOptions
            {
                Url = "https://hooks.example.com/alert",
                TimeoutSeconds = 5
            }
        });

        var logger = Substitute.For<ILogger<WebhookAlertChannel>>();
        var channel = new WebhookAlertChannel(factory, options, logger);
        var payload = CreateTestPayload();

        var result = await channel.SendAsync(payload);

        result.Should().BeTrue();
        handler.LastRequestUri.Should().Be("https://hooks.example.com/alert");
        handler.LastRequestBody.Should().Contain("Webhook Test");
    }

    [Fact]
    public async Task SendAsync_WhenServerReturns500_ReturnsFalse()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.InternalServerError);
        var httpClient = new HttpClient(handler);

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(WebhookAlertChannel.HttpClientName).Returns(httpClient);

        var options = Options.Create(new AlertingOptions
        {
            Webhook = new WebhookChannelOptions
            {
                Url = "https://hooks.example.com/alert",
                TimeoutSeconds = 5
            }
        });

        var logger = Substitute.For<ILogger<WebhookAlertChannel>>();
        var channel = new WebhookAlertChannel(factory, options, logger);

        var result = await channel.SendAsync(CreateTestPayload());

        result.Should().BeFalse();
    }

    /// <summary>
    /// Mock HttpMessageHandler para testes unitários de chamadas HTTP.
    /// </summary>
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;

        public string? LastRequestUri { get; private set; }
        public string? LastRequestBody { get; private set; }

        public MockHttpMessageHandler(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri?.ToString();
            if (request.Content is not null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return new HttpResponseMessage(_statusCode);
        }
    }
}
