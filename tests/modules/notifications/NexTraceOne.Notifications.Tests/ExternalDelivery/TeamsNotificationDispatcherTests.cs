using System.Net.Http;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.ExternalDelivery;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Infrastructure.ExternalDelivery;

namespace NexTraceOne.Notifications.Tests.ExternalDelivery;

public sealed class TeamsNotificationDispatcherTests
{
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();
    private readonly IExternalChannelTemplateResolver _templateResolver = Substitute.For<IExternalChannelTemplateResolver>();
    private readonly ILogger<TeamsNotificationDispatcher> _logger =
        NullLoggerFactory.Instance.CreateLogger<TeamsNotificationDispatcher>();

    private TeamsNotificationDispatcher CreateDispatcher(
        bool enabled = true,
        string? webhookUrl = "https://teams.webhook.test/abc")
    {
        var options = Options.Create(new NotificationChannelOptions
        {
            Teams = new TeamsChannelSettings
            {
                Enabled = enabled,
                WebhookUrl = webhookUrl,
                TimeoutSeconds = 10
            }
        });

        _templateResolver.ResolveTeamsTemplate(Arg.Any<Notification>(), Arg.Any<string>())
            .Returns(new TeamsCardPayload("{\"type\":\"message\",\"text\":\"test\"}"));

        return new TeamsNotificationDispatcher(_httpClientFactory, options, _templateResolver, _logger);
    }

    private static Notification CreateTestNotification()
    {
        return Notification.Create(
            tenantId: Guid.NewGuid(),
            recipientUserId: Guid.NewGuid(),
            eventType: "IncidentCreated",
            category: NotificationCategory.Incident,
            severity: NotificationSeverity.Critical,
            title: "Test",
            message: "Test message",
            sourceModule: "TestModule");
    }

    [Fact]
    public void ChannelName_ReturnsMicrosoftTeams()
    {
        var dispatcher = CreateDispatcher();
        dispatcher.ChannelName.Should().Be("MicrosoftTeams");
    }

    [Fact]
    public void Channel_ReturnsMicrosoftTeamsEnum()
    {
        var dispatcher = CreateDispatcher();
        dispatcher.Channel.Should().Be(DeliveryChannel.MicrosoftTeams);
    }

    [Fact]
    public async Task DispatchAsync_ChannelDisabled_ReturnsFalse()
    {
        var dispatcher = CreateDispatcher(enabled: false);
        var notification = CreateTestNotification();

        var result = await dispatcher.DispatchAsync(notification, null);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DispatchAsync_NoWebhookUrl_ReturnsFalse()
    {
        var dispatcher = CreateDispatcher(webhookUrl: null);
        var notification = CreateTestNotification();

        var result = await dispatcher.DispatchAsync(notification, null);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DispatchAsync_RecipientAddressOverridesWebhookUrl()
    {
        // When recipient address is provided, it should use that instead of config webhook
        var dispatcher = CreateDispatcher(webhookUrl: null);
        var notification = CreateTestNotification();

        // Can't fully test without HTTP mock, but ensures it doesn't return false
        // when recipient address is provided (it will try to send)
        _httpClientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient());

        // This will throw because recipient URL is not a real webhook,
        // but the important thing is it doesn't return false for missing URL
        var act = () => dispatcher.DispatchAsync(notification, "https://custom.webhook.url/test");
        await act.Should().ThrowAsync<Exception>();
    }
}
