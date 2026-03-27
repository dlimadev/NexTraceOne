using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.ExternalDelivery;
using NexTraceOne.Notifications.Application.Features.GetNotificationTrail;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Tests.Application;

/// <summary>
/// Testes para GetNotificationTrail — rastreabilidade auditável de notificações.
/// P7.3: valida correlação origem → notificação → entregas → trail.
/// </summary>
public sealed class NotificationTrailHandlerTests
{
    private readonly INotificationStore _notificationStore = Substitute.For<INotificationStore>();
    private readonly INotificationDeliveryStore _deliveryStore = Substitute.For<INotificationDeliveryStore>();

    private GetNotificationTrail.Handler CreateHandler() =>
        new(_notificationStore, _deliveryStore);

    private static Notification CreateTestNotification(
        string? sourceEventId = null,
        string sourceEntityId = "incident-123")
    {
        var notification = Notification.Create(
            tenantId: Guid.NewGuid(),
            recipientUserId: Guid.NewGuid(),
            eventType: "IncidentCreated",
            category: NotificationCategory.Incident,
            severity: NotificationSeverity.Critical,
            title: "Incident Created",
            message: "A critical incident was created.",
            sourceModule: "OperationalIntelligence",
            sourceEntityType: "Incident",
            sourceEntityId: sourceEntityId,
            sourceEventId: sourceEventId);
        return notification;
    }

    private static NotificationDelivery CreateDelivery(NotificationId notificationId, DeliveryChannel channel)
        => NotificationDelivery.Create(notificationId, channel, recipientAddress: "user@example.com");

    [Fact]
    public async Task Handle_NotificationNotFound_ReturnsError()
    {
        var notificationId = Guid.NewGuid();
        _notificationStore.GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns((Notification?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetNotificationTrail.Query(notificationId), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Notification.NotFound");
    }

    [Fact]
    public async Task Handle_NotificationFound_NoDeliveries_ReturnsTrailWithZeroAttempts()
    {
        var notification = CreateTestNotification(sourceEventId: "evt-456");
        _notificationStore.GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns(notification);
        _deliveryStore.ListByNotificationIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<NotificationDelivery>() as IReadOnlyList<NotificationDelivery>);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetNotificationTrail.Query(notification.Id.Value), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.NotificationId.Should().Be(notification.Id.Value);
        result.Value.TotalDeliveryAttempts.Should().Be(0);
        result.Value.Deliveries.Should().BeEmpty();
        result.Value.IsDeliveredToAnyChannel.Should().BeFalse();
        result.Value.HasPendingRetry.Should().BeFalse();
        result.Value.HasPermanentFailure.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_SourceEventIdPreservedInTrail()
    {
        const string sourceEventId = "evt-correlation-789";
        var notification = CreateTestNotification(sourceEventId: sourceEventId);
        _notificationStore.GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns(notification);
        _deliveryStore.ListByNotificationIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<NotificationDelivery>() as IReadOnlyList<NotificationDelivery>);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetNotificationTrail.Query(notification.Id.Value), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Notification.SourceEventId.Should().Be(sourceEventId);
        result.Value.Notification.SourceModule.Should().Be("OperationalIntelligence");
        result.Value.Notification.EventType.Should().Be("IncidentCreated");
    }

    [Fact]
    public async Task Handle_NotificationWithDeliveredAttempt_ReturnsIsDeliveredTrue()
    {
        var notification = CreateTestNotification(sourceEventId: "evt-001");
        var delivery = CreateDelivery(notification.Id, DeliveryChannel.Email);
        delivery.IncrementRetry();
        delivery.MarkDelivered();

        _notificationStore.GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns(notification);
        _deliveryStore.ListByNotificationIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns(new[] { delivery } as IReadOnlyList<NotificationDelivery>);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetNotificationTrail.Query(notification.Id.Value), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsDeliveredToAnyChannel.Should().BeTrue();
        result.Value.Deliveries.Should().HaveCount(1);
        result.Value.Deliveries[0].Status.Should().Be("Delivered");
        result.Value.TotalDeliveryAttempts.Should().Be(1);
    }

    [Fact]
    public async Task Handle_NotificationWithRetryScheduled_ReturnsHasPendingRetryTrue()
    {
        var notification = CreateTestNotification();
        var delivery = CreateDelivery(notification.Id, DeliveryChannel.Email);
        delivery.IncrementRetry();
        delivery.ScheduleRetry(DateTimeOffset.UtcNow.AddMinutes(5), "SMTP connection timeout");

        _notificationStore.GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns(notification);
        _deliveryStore.ListByNotificationIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns(new[] { delivery } as IReadOnlyList<NotificationDelivery>);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetNotificationTrail.Query(notification.Id.Value), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasPendingRetry.Should().BeTrue();
        result.Value.Deliveries[0].ErrorMessage.Should().Be("SMTP connection timeout");
    }

    [Fact]
    public async Task Handle_NotificationWithPermanentFailure_ReturnsHasPermanentFailureTrue()
    {
        var notification = CreateTestNotification();
        var delivery = CreateDelivery(notification.Id, DeliveryChannel.Email);
        delivery.IncrementRetry();
        delivery.MarkFailed("Max attempts reached");

        _notificationStore.GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns(notification);
        _deliveryStore.ListByNotificationIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns(new[] { delivery } as IReadOnlyList<NotificationDelivery>);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetNotificationTrail.Query(notification.Id.Value), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasPermanentFailure.Should().BeTrue();
        result.Value.Deliveries[0].FailedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_MultipleChannelDeliveries_ReturnsAllInTrail()
    {
        var notification = CreateTestNotification(sourceEventId: "evt-multi");
        var emailDelivery = CreateDelivery(notification.Id, DeliveryChannel.Email);
        emailDelivery.IncrementRetry();
        emailDelivery.MarkDelivered();

        var teamsDelivery = CreateDelivery(notification.Id, DeliveryChannel.MicrosoftTeams);
        teamsDelivery.IncrementRetry();
        teamsDelivery.ScheduleRetry(DateTimeOffset.UtcNow.AddMinutes(2), "Teams webhook error");

        _notificationStore.GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns(notification);
        _deliveryStore.ListByNotificationIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns(new[] { emailDelivery, teamsDelivery } as IReadOnlyList<NotificationDelivery>);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetNotificationTrail.Query(notification.Id.Value), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Deliveries.Should().HaveCount(2);
        result.Value.TotalDeliveryAttempts.Should().Be(2);
        result.Value.IsDeliveredToAnyChannel.Should().BeTrue();
        result.Value.HasPendingRetry.Should().BeTrue();
        result.Value.HasPermanentFailure.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NullSourceEventId_IsAllowedInTrail()
    {
        var notification = CreateTestNotification(sourceEventId: null);
        _notificationStore.GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns(notification);
        _deliveryStore.ListByNotificationIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<NotificationDelivery>() as IReadOnlyList<NotificationDelivery>);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetNotificationTrail.Query(notification.Id.Value), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Notification.SourceEventId.Should().BeNull();
    }
}
