using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.ExternalDelivery;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Infrastructure.ExternalDelivery;

namespace NexTraceOne.Notifications.Tests.ExternalDelivery;

public sealed class EmailNotificationDispatcherTests
{
    private readonly IExternalChannelTemplateResolver _templateResolver = Substitute.For<IExternalChannelTemplateResolver>();
    private readonly ILogger<EmailNotificationDispatcher> _logger =
        NullLoggerFactory.Instance.CreateLogger<EmailNotificationDispatcher>();

    private EmailNotificationDispatcher CreateDispatcher(
        bool enabled = true,
        string? smtpHost = "smtp.test.com",
        string? fromAddress = "noreply@test.com")
    {
        var options = Options.Create(new NotificationChannelOptions
        {
            Email = new EmailChannelSettings
            {
                Enabled = enabled,
                SmtpHost = smtpHost,
                SmtpPort = 587,
                FromAddress = fromAddress,
                FromName = "NexTraceOne Test"
            }
        });

        _templateResolver.ResolveEmailTemplate(Arg.Any<Notification>(), Arg.Any<string>())
            .Returns(new EmailTemplate("Test Subject", "<html>Test Body</html>", "Test Body"));

        return new EmailNotificationDispatcher(options, _templateResolver, _logger);
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
    public void ChannelName_ReturnsEmail()
    {
        var dispatcher = CreateDispatcher();
        dispatcher.ChannelName.Should().Be("Email");
    }

    [Fact]
    public void Channel_ReturnsEmailEnum()
    {
        var dispatcher = CreateDispatcher();
        dispatcher.Channel.Should().Be(DeliveryChannel.Email);
    }

    [Fact]
    public async Task DispatchAsync_ChannelDisabled_ReturnsFalse()
    {
        var dispatcher = CreateDispatcher(enabled: false);
        var notification = CreateTestNotification();

        var result = await dispatcher.DispatchAsync(notification, "user@test.com");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DispatchAsync_NoRecipientAddress_ReturnsFalse()
    {
        var dispatcher = CreateDispatcher();
        var notification = CreateTestNotification();

        var result = await dispatcher.DispatchAsync(notification, null);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DispatchAsync_EmptyRecipientAddress_ReturnsFalse()
    {
        var dispatcher = CreateDispatcher();
        var notification = CreateTestNotification();

        var result = await dispatcher.DispatchAsync(notification, "   ");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DispatchAsync_NoSmtpHost_ReturnsFalse()
    {
        var dispatcher = CreateDispatcher(smtpHost: null);
        var notification = CreateTestNotification();

        var result = await dispatcher.DispatchAsync(notification, "user@test.com");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DispatchAsync_NoFromAddress_ReturnsFalse()
    {
        var dispatcher = CreateDispatcher(fromAddress: null);
        var notification = CreateTestNotification();

        var result = await dispatcher.DispatchAsync(notification, "user@test.com");

        result.Should().BeFalse();
    }
}
