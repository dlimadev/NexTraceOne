using System.Net.Http;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.ExternalDelivery;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;
using NexTraceOne.Notifications.Infrastructure.ExternalDelivery;

namespace NexTraceOne.Notifications.Tests.ExternalDelivery;

public sealed class ExternalDeliveryServiceTests
{
    private readonly INotificationRoutingEngine _routingEngine = Substitute.For<INotificationRoutingEngine>();
    private readonly INotificationChannelDispatcher _emailDispatcher = Substitute.For<INotificationChannelDispatcher>();
    private readonly INotificationChannelDispatcher _teamsDispatcher = Substitute.For<INotificationChannelDispatcher>();
    private readonly INotificationDeliveryStore _deliveryStore = Substitute.For<INotificationDeliveryStore>();
    private readonly IOptions<DeliveryRetryOptions> _retryOptions;
    private readonly ILogger<ExternalDeliveryService> _logger =
        NullLoggerFactory.Instance.CreateLogger<ExternalDeliveryService>();
    private readonly ExternalDeliveryService _service;

    public ExternalDeliveryServiceTests()
    {
        _emailDispatcher.Channel.Returns(DeliveryChannel.Email);
        _emailDispatcher.ChannelName.Returns("Email");

        _teamsDispatcher.Channel.Returns(DeliveryChannel.MicrosoftTeams);
        _teamsDispatcher.ChannelName.Returns("MicrosoftTeams");

        _retryOptions = Options.Create(new DeliveryRetryOptions
        {
            MaxAttempts = 3,
            BaseDelaySeconds = 0 // Zero delay in tests
        });

        _service = new ExternalDeliveryService(
            _routingEngine,
            [_emailDispatcher, _teamsDispatcher],
            _deliveryStore,
            _retryOptions,
            _logger);
    }

    private static Notification CreateTestNotification(
        NotificationSeverity severity = NotificationSeverity.Critical)
    {
        return Notification.Create(
            tenantId: Guid.NewGuid(),
            recipientUserId: Guid.NewGuid(),
            eventType: "IncidentCreated",
            category: NotificationCategory.Incident,
            severity: severity,
            title: "Test notification",
            message: "Test message",
            sourceModule: "TestModule");
    }

    [Fact]
    public async Task ProcessExternalDeliveryAsync_NoExternalChannels_DoesNotDispatch()
    {
        var notification = CreateTestNotification(NotificationSeverity.Info);
        _routingEngine.ResolveChannelsAsync(
            Arg.Any<Guid>(), Arg.Any<NotificationCategory>(),
            Arg.Any<NotificationSeverity>(), Arg.Any<CancellationToken>())
            .Returns([DeliveryChannel.InApp]);

        await _service.ProcessExternalDeliveryAsync(notification);

        await _emailDispatcher.DidNotReceive().DispatchAsync(
            Arg.Any<Notification>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await _teamsDispatcher.DidNotReceive().DispatchAsync(
            Arg.Any<Notification>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessExternalDeliveryAsync_EmailOnly_DispatchesToEmail()
    {
        var notification = CreateTestNotification();
        _routingEngine.ResolveChannelsAsync(
            Arg.Any<Guid>(), Arg.Any<NotificationCategory>(),
            Arg.Any<NotificationSeverity>(), Arg.Any<CancellationToken>())
            .Returns([DeliveryChannel.InApp, DeliveryChannel.Email]);

        _emailDispatcher.DispatchAsync(
            Arg.Any<Notification>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await _service.ProcessExternalDeliveryAsync(notification);

        await _emailDispatcher.Received(1).DispatchAsync(
            Arg.Any<Notification>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await _teamsDispatcher.DidNotReceive().DispatchAsync(
            Arg.Any<Notification>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessExternalDeliveryAsync_AllChannels_DispatchesToBoth()
    {
        var notification = CreateTestNotification();
        _routingEngine.ResolveChannelsAsync(
            Arg.Any<Guid>(), Arg.Any<NotificationCategory>(),
            Arg.Any<NotificationSeverity>(), Arg.Any<CancellationToken>())
            .Returns([DeliveryChannel.InApp, DeliveryChannel.Email, DeliveryChannel.MicrosoftTeams]);

        _emailDispatcher.DispatchAsync(
            Arg.Any<Notification>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _teamsDispatcher.DispatchAsync(
            Arg.Any<Notification>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await _service.ProcessExternalDeliveryAsync(notification);

        await _emailDispatcher.Received(1).DispatchAsync(
            Arg.Any<Notification>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await _teamsDispatcher.Received(1).DispatchAsync(
            Arg.Any<Notification>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessExternalDeliveryAsync_SuccessfulDelivery_CreatesDeliveryRecord()
    {
        var notification = CreateTestNotification();
        _routingEngine.ResolveChannelsAsync(
            Arg.Any<Guid>(), Arg.Any<NotificationCategory>(),
            Arg.Any<NotificationSeverity>(), Arg.Any<CancellationToken>())
            .Returns([DeliveryChannel.InApp, DeliveryChannel.Email]);

        _emailDispatcher.DispatchAsync(
            Arg.Any<Notification>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await _service.ProcessExternalDeliveryAsync(notification);

        await _deliveryStore.Received(1).AddAsync(
            Arg.Is<NotificationDelivery>(d => d.Channel == DeliveryChannel.Email),
            Arg.Any<CancellationToken>());
        await _deliveryStore.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessExternalDeliveryAsync_DispatcherReturnsFalse_MarksSkipped()
    {
        var notification = CreateTestNotification();
        _routingEngine.ResolveChannelsAsync(
            Arg.Any<Guid>(), Arg.Any<NotificationCategory>(),
            Arg.Any<NotificationSeverity>(), Arg.Any<CancellationToken>())
            .Returns([DeliveryChannel.InApp, DeliveryChannel.Email]);

        _emailDispatcher.DispatchAsync(
            Arg.Any<Notification>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await _service.ProcessExternalDeliveryAsync(notification);

        await _deliveryStore.Received(1).AddAsync(
            Arg.Any<NotificationDelivery>(), Arg.Any<CancellationToken>());
        await _deliveryStore.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessExternalDeliveryAsync_TransientFailure_RetriesAndSucceeds()
    {
        var notification = CreateTestNotification();
        _routingEngine.ResolveChannelsAsync(
            Arg.Any<Guid>(), Arg.Any<NotificationCategory>(),
            Arg.Any<NotificationSeverity>(), Arg.Any<CancellationToken>())
            .Returns([DeliveryChannel.InApp, DeliveryChannel.Email]);

        // First call fails, second succeeds
        _emailDispatcher.DispatchAsync(
            Arg.Any<Notification>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(
                _ => throw new HttpRequestException("Temporary failure"),
                _ => Task.FromResult(true));

        await _service.ProcessExternalDeliveryAsync(notification);

        await _emailDispatcher.Received(2).DispatchAsync(
            Arg.Any<Notification>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessExternalDeliveryAsync_PermanentFailure_StopsAfterMaxRetries()
    {
        var notification = CreateTestNotification();
        _routingEngine.ResolveChannelsAsync(
            Arg.Any<Guid>(), Arg.Any<NotificationCategory>(),
            Arg.Any<NotificationSeverity>(), Arg.Any<CancellationToken>())
            .Returns([DeliveryChannel.InApp, DeliveryChannel.Email]);

        _emailDispatcher.DispatchAsync(
            Arg.Any<Notification>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns<bool>(_ => throw new HttpRequestException("Permanent failure"));

        await _service.ProcessExternalDeliveryAsync(notification);

        // Should attempt MaxAttempts=3 times
        await _emailDispatcher.Received(3).DispatchAsync(
            Arg.Any<Notification>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessExternalDeliveryAsync_OneChannelFails_OtherStillDelivers()
    {
        var notification = CreateTestNotification();
        _routingEngine.ResolveChannelsAsync(
            Arg.Any<Guid>(), Arg.Any<NotificationCategory>(),
            Arg.Any<NotificationSeverity>(), Arg.Any<CancellationToken>())
            .Returns([DeliveryChannel.InApp, DeliveryChannel.Email, DeliveryChannel.MicrosoftTeams]);

        _emailDispatcher.DispatchAsync(
            Arg.Any<Notification>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns<bool>(_ => throw new HttpRequestException("Email failure"));
        _teamsDispatcher.DispatchAsync(
            Arg.Any<Notification>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await _service.ProcessExternalDeliveryAsync(notification);

        // Teams should still be dispatched successfully
        await _teamsDispatcher.Received(1).DispatchAsync(
            Arg.Any<Notification>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }
}
