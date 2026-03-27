using NexTraceOne.Notifications.Application.ExternalDelivery;
using NexTraceOne.Notifications.Application.Features.GetDeliveryHistory;
using NexTraceOne.Notifications.Application.Features.GetDeliveryStatus;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Tests.Application;

/// <summary>
/// Testes de unidade para os handlers GetDeliveryHistory e GetDeliveryStatus (P7.2).
/// </summary>
public sealed class DeliveryHistoryHandlerTests
{
    private readonly INotificationDeliveryStore _deliveryStore = Substitute.For<INotificationDeliveryStore>();

    // ── GetDeliveryHistory ──────────────────────────────────────────────────

    [Fact]
    public async Task GetDeliveryHistory_NoDeliveries_ReturnsEmptyResponse()
    {
        var notificationId = Guid.NewGuid();
        _deliveryStore.ListByNotificationIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<NotificationDelivery>)[]);

        var handler = new GetDeliveryHistory.Handler(_deliveryStore, Substitute.For<NexTraceOne.BuildingBlocks.Application.Abstractions.ICurrentUser>());
        var result = await handler.Handle(new GetDeliveryHistory.Query(notificationId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.NotificationId.Should().Be(notificationId);
        result.Value.Deliveries.Should().BeEmpty();
        result.Value.TotalAttempts.Should().Be(0);
        result.Value.HasSuccessfulDelivery.Should().BeFalse();
    }

    [Fact]
    public async Task GetDeliveryHistory_WithDeliveries_ReturnsMappedDtos()
    {
        var notificationId = new NotificationId(Guid.NewGuid());
        var delivery1 = NotificationDelivery.Create(notificationId, DeliveryChannel.Email, "a@test.com");
        delivery1.IncrementRetry();
        delivery1.MarkDelivered();

        var delivery2 = NotificationDelivery.Create(notificationId, DeliveryChannel.MicrosoftTeams);
        delivery2.IncrementRetry();
        delivery2.ScheduleRetry(DateTimeOffset.UtcNow.AddSeconds(30), "Teams error");

        _deliveryStore.ListByNotificationIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<NotificationDelivery>)[delivery1, delivery2]);

        var handler = new GetDeliveryHistory.Handler(_deliveryStore, Substitute.For<NexTraceOne.BuildingBlocks.Application.Abstractions.ICurrentUser>());
        var result = await handler.Handle(new GetDeliveryHistory.Query(notificationId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Deliveries.Should().HaveCount(2);
        result.Value.HasSuccessfulDelivery.Should().BeTrue();
        result.Value.TotalAttempts.Should().Be(2); // 1 + 1 attempts

        var emailDto = result.Value.Deliveries.First(d => d.Channel == "Email");
        emailDto.Status.Should().Be("Delivered");
        emailDto.DeliveredAt.Should().NotBeNull();

        var teamsDto = result.Value.Deliveries.First(d => d.Channel == "MicrosoftTeams");
        teamsDto.Status.Should().Be("RetryScheduled");
        teamsDto.NextRetryAt.Should().NotBeNull();
        teamsDto.ErrorMessage.Should().Be("Teams error");
    }

    [Fact]
    public async Task GetDeliveryHistory_AllFailed_HasSuccessfulDeliveryIsFalse()
    {
        var notificationId = new NotificationId(Guid.NewGuid());
        var delivery = NotificationDelivery.Create(notificationId, DeliveryChannel.Email);
        delivery.IncrementRetry();
        delivery.MarkFailed("permanent error");

        _deliveryStore.ListByNotificationIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<NotificationDelivery>)[delivery]);

        var handler = new GetDeliveryHistory.Handler(_deliveryStore, Substitute.For<NexTraceOne.BuildingBlocks.Application.Abstractions.ICurrentUser>());
        var result = await handler.Handle(new GetDeliveryHistory.Query(notificationId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasSuccessfulDelivery.Should().BeFalse();
    }

    // ── GetDeliveryStatus ───────────────────────────────────────────────────

    [Fact]
    public async Task GetDeliveryStatus_NoDeliveries_ReturnsAllFalse()
    {
        var notificationId = Guid.NewGuid();
        _deliveryStore.ListByNotificationIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<NotificationDelivery>)[]);

        var handler = new GetDeliveryStatus.Handler(_deliveryStore);
        var result = await handler.Handle(new GetDeliveryStatus.Query(notificationId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsDeliveredToAnyChannel.Should().BeFalse();
        result.Value.HasPendingRetry.Should().BeFalse();
        result.Value.HasPermanentFailure.Should().BeFalse();
        result.Value.TotalChannelAttempts.Should().Be(0);
        result.Value.ChannelStatuses.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDeliveryStatus_DeliveredToOneChannel_ReturnsIsDeliveredTrue()
    {
        var notificationId = new NotificationId(Guid.NewGuid());
        var delivery = NotificationDelivery.Create(notificationId, DeliveryChannel.Email, "x@test.com");
        delivery.IncrementRetry();
        delivery.MarkDelivered();

        _deliveryStore.ListByNotificationIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<NotificationDelivery>)[delivery]);

        var handler = new GetDeliveryStatus.Handler(_deliveryStore);
        var result = await handler.Handle(new GetDeliveryStatus.Query(notificationId.Value), CancellationToken.None);

        result.Value.IsDeliveredToAnyChannel.Should().BeTrue();
        result.Value.HasPendingRetry.Should().BeFalse();
        result.Value.HasPermanentFailure.Should().BeFalse();
        result.Value.TotalChannelAttempts.Should().Be(1);
    }

    [Fact]
    public async Task GetDeliveryStatus_HasRetryScheduled_ReturnsHasPendingRetryTrue()
    {
        var notificationId = new NotificationId(Guid.NewGuid());
        var delivery = NotificationDelivery.Create(notificationId, DeliveryChannel.Email);
        delivery.IncrementRetry();
        delivery.ScheduleRetry(DateTimeOffset.UtcNow.AddSeconds(60), "error");

        _deliveryStore.ListByNotificationIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<NotificationDelivery>)[delivery]);

        var handler = new GetDeliveryStatus.Handler(_deliveryStore);
        var result = await handler.Handle(new GetDeliveryStatus.Query(notificationId.Value), CancellationToken.None);

        result.Value.HasPendingRetry.Should().BeTrue();
        result.Value.IsDeliveredToAnyChannel.Should().BeFalse();
    }

    [Fact]
    public async Task GetDeliveryStatus_PermanentFailure_ReturnsHasPermanentFailureTrue()
    {
        var notificationId = new NotificationId(Guid.NewGuid());
        var delivery = NotificationDelivery.Create(notificationId, DeliveryChannel.Email);
        delivery.IncrementRetry();
        delivery.MarkFailed("Permanent error");

        _deliveryStore.ListByNotificationIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<NotificationDelivery>)[delivery]);

        var handler = new GetDeliveryStatus.Handler(_deliveryStore);
        var result = await handler.Handle(new GetDeliveryStatus.Query(notificationId.Value), CancellationToken.None);

        result.Value.HasPermanentFailure.Should().BeTrue();
        result.Value.IsDeliveredToAnyChannel.Should().BeFalse();
    }
}
