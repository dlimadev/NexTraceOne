using System.Net.Http;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.ExternalDelivery;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
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
    private readonly INotificationAuditService _auditService = Substitute.For<INotificationAuditService>();
    private readonly IEnvironmentBehaviorService _envBehavior = Substitute.For<IEnvironmentBehaviorService>();
    private readonly IQuietHoursService _quietHours = Substitute.For<IQuietHoursService>();
    private readonly IMandatoryNotificationPolicy _mandatoryPolicy = Substitute.For<IMandatoryNotificationPolicy>();
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

        // Fail-open: external channels enabled
        _envBehavior.IsEnabledAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Fail-open: not in quiet hours (always deliver)
        _quietHours.ShouldDeferAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Default mandatory policy: nothing is mandatory
        _mandatoryPolicy.IsMandatory(Arg.Any<string>(), Arg.Any<NotificationCategory>(), Arg.Any<NotificationSeverity>())
            .Returns(false);

        _service = new ExternalDeliveryService(
            _routingEngine,
            [_emailDispatcher, _teamsDispatcher],
            _deliveryStore,
            _auditService,
            _envBehavior,
            _quietHours,
            _mandatoryPolicy,
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
    public async Task ProcessExternalDeliveryAsync_TransientFailure_SchedulesRetryInstead()
    {
        // P7.2 behavior: on failure, delivery is scheduled for deferred retry (not inline retry)
        var notification = CreateTestNotification();

        NotificationDelivery? capturedDelivery = null;
        _deliveryStore
            .When(s => s.AddAsync(Arg.Any<NotificationDelivery>(), Arg.Any<CancellationToken>()))
            .Do(ci => capturedDelivery = ci.Arg<NotificationDelivery>());

        _routingEngine.ResolveChannelsAsync(
            Arg.Any<Guid>(), Arg.Any<NotificationCategory>(),
            Arg.Any<NotificationSeverity>(), Arg.Any<CancellationToken>())
            .Returns([DeliveryChannel.InApp, DeliveryChannel.Email]);

        _emailDispatcher.DispatchAsync(
            Arg.Any<Notification>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns<bool>(_ => throw new HttpRequestException("Temporary failure"));

        await _service.ProcessExternalDeliveryAsync(notification);

        // P7.2: Only 1 dispatch attempt per ProcessExternalDelivery call; retry is deferred
        await _emailDispatcher.Received(1).DispatchAsync(
            Arg.Any<Notification>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());

        // Delivery record must be persisted and have RetryScheduled status with NextRetryAt
        capturedDelivery.Should().NotBeNull();
        capturedDelivery!.Status.Should().Be(DeliveryStatus.RetryScheduled);
        capturedDelivery.NextRetryAt.Should().NotBeNull();
        capturedDelivery.NextRetryAt.Should().BeOnOrAfter(DateTimeOffset.UtcNow.AddSeconds(-5));
        await _deliveryStore.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RetryDeliveryAsync_OnRetry_DispatchesAgainAndMarksDelivered()
    {
        // P7.2: RetryDeliveryAsync is called by the retry job for a previously-scheduled delivery
        var notification = CreateTestNotification();
        var delivery = NotificationDelivery.Create(notification.Id, DeliveryChannel.Email, "test@example.com");
        delivery.ScheduleRetry(DateTimeOffset.UtcNow.AddMinutes(-1), "Previous transient error");

        _emailDispatcher.DispatchAsync(
            Arg.Any<Notification>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await _service.RetryDeliveryAsync(delivery, notification);

        delivery.Status.Should().Be(DeliveryStatus.Delivered);
        await _deliveryStore.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RetryDeliveryAsync_PermanentFailureOnLastAttempt_MarksDeliveryAsFailed()
    {
        var notification = CreateTestNotification();
        var delivery = NotificationDelivery.Create(notification.Id, DeliveryChannel.Email, "test@example.com");

        // Simulate already at MaxAttempts-1
        for (var i = 0; i < _retryOptions.Value.MaxAttempts - 1; i++)
            delivery.IncrementRetry();

        delivery.ScheduleRetry(DateTimeOffset.UtcNow.AddMinutes(-1), "Previous transient error");

        _emailDispatcher.DispatchAsync(
            Arg.Any<Notification>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns<bool>(_ => throw new HttpRequestException("Still failing"));

        await _service.RetryDeliveryAsync(delivery, notification);

        delivery.Status.Should().Be(DeliveryStatus.Failed);
        await _deliveryStore.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessExternalDeliveryAsync_PermanentFailureAtMaxAttempts_MarksAsFailed()
    {
        // P7.2: If already at MaxAttempts (via previous retries), marks as permanently failed
        var notification = CreateTestNotification();
        _routingEngine.ResolveChannelsAsync(
            Arg.Any<Guid>(), Arg.Any<NotificationCategory>(),
            Arg.Any<NotificationSeverity>(), Arg.Any<CancellationToken>())
            .Returns([DeliveryChannel.InApp, DeliveryChannel.Email]);

        _emailDispatcher.DispatchAsync(
            Arg.Any<Notification>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns<bool>(_ => throw new HttpRequestException("Permanent failure"));

        await _service.ProcessExternalDeliveryAsync(notification);

        // First attempt: RetryCount becomes 1; MaxAttempts is 3 → scheduled for retry, not failed yet
        await _deliveryStore.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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
