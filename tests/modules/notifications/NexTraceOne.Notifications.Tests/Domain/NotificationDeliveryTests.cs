using FluentAssertions;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Tests.Domain;

/// <summary>
/// Testes de unidade para a entidade NotificationDelivery.
/// Valida criação, transições de estado, retry inline e retry deferido (P7.2).
/// </summary>
public sealed class NotificationDeliveryTests
{
    [Fact]
    public void Create_ValidParameters_ShouldCreateDelivery()
    {
        var notificationId = new NexTraceOne.Notifications.Domain.StronglyTypedIds.NotificationId(Guid.NewGuid());

        var delivery = NotificationDelivery.Create(
            notificationId, DeliveryChannel.Email, "user@example.com");

        delivery.Should().NotBeNull();
        delivery.Id.Value.Should().NotBeEmpty();
        delivery.NotificationId.Should().Be(notificationId);
        delivery.Channel.Should().Be(DeliveryChannel.Email);
        delivery.RecipientAddress.Should().Be("user@example.com");
        delivery.Status.Should().Be(DeliveryStatus.Pending);
        delivery.RetryCount.Should().Be(0);
        delivery.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        delivery.LastAttemptAt.Should().BeNull();
        delivery.NextRetryAt.Should().BeNull();
    }

    [Fact]
    public void Create_TeamsChannel_ShouldWork()
    {
        var notificationId = new NexTraceOne.Notifications.Domain.StronglyTypedIds.NotificationId(Guid.NewGuid());

        var delivery = NotificationDelivery.Create(
            notificationId, DeliveryChannel.MicrosoftTeams, "teams-channel-id");

        delivery.Channel.Should().Be(DeliveryChannel.MicrosoftTeams);
    }

    [Fact]
    public void MarkDelivered_ShouldTransitionToDelivered()
    {
        var delivery = CreateTestDelivery();

        delivery.MarkDelivered();

        delivery.Status.Should().Be(DeliveryStatus.Delivered);
        delivery.DeliveredAt.Should().NotBeNull();
        delivery.LastAttemptAt.Should().NotBeNull();
        delivery.NextRetryAt.Should().BeNull();
    }

    [Fact]
    public void MarkFailed_ShouldTransitionToFailed()
    {
        var delivery = CreateTestDelivery();

        delivery.MarkFailed("SMTP connection refused");

        delivery.Status.Should().Be(DeliveryStatus.Failed);
        delivery.FailedAt.Should().NotBeNull();
        delivery.LastAttemptAt.Should().NotBeNull();
        delivery.ErrorMessage.Should().Be("SMTP connection refused");
        delivery.NextRetryAt.Should().BeNull();
    }

    [Fact]
    public void MarkFailed_WithNullError_ShouldWork()
    {
        var delivery = CreateTestDelivery();

        delivery.MarkFailed(null);

        delivery.Status.Should().Be(DeliveryStatus.Failed);
        delivery.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void MarkSkipped_ShouldTransitionToSkipped()
    {
        var delivery = CreateTestDelivery();

        delivery.MarkSkipped();

        delivery.Status.Should().Be(DeliveryStatus.Skipped);
        delivery.LastAttemptAt.Should().NotBeNull();
        delivery.NextRetryAt.Should().BeNull();
    }

    [Fact]
    public void IncrementRetry_ShouldIncrementCountAndResetToPending()
    {
        var delivery = CreateTestDelivery();
        delivery.ScheduleRetry(DateTimeOffset.UtcNow.AddSeconds(30), "Timeout");

        delivery.IncrementRetry();

        delivery.RetryCount.Should().Be(1);
        delivery.Status.Should().Be(DeliveryStatus.Pending);
        delivery.NextRetryAt.Should().BeNull();
        delivery.LastAttemptAt.Should().NotBeNull();
    }

    [Fact]
    public void IncrementRetry_MultipleTimes_ShouldAccumulate()
    {
        var delivery = CreateTestDelivery();

        delivery.IncrementRetry();
        delivery.IncrementRetry();
        delivery.IncrementRetry();

        delivery.RetryCount.Should().Be(3);
    }

    // ── P7.2: ScheduleRetry ────────────────────────────────────────────────

    [Fact]
    public void ScheduleRetry_ShouldSetRetryScheduledStatusAndNextRetryAt()
    {
        var delivery = CreateTestDelivery();
        var nextRetryAt = DateTimeOffset.UtcNow.AddSeconds(60);

        delivery.ScheduleRetry(nextRetryAt, "Connection timeout");

        delivery.Status.Should().Be(DeliveryStatus.RetryScheduled);
        delivery.NextRetryAt.Should().Be(nextRetryAt);
        delivery.ErrorMessage.Should().Be("Connection timeout");
        delivery.FailedAt.Should().BeNull(); // FailedAt is only set on permanent failure (MarkFailed)
        delivery.LastAttemptAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ScheduleRetry_WithoutError_ShouldStillSetStatus()
    {
        var delivery = CreateTestDelivery();

        delivery.ScheduleRetry(DateTimeOffset.UtcNow.AddSeconds(30));

        delivery.Status.Should().Be(DeliveryStatus.RetryScheduled);
        delivery.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void FullRetryLifecycle_EventuallyDelivered()
    {
        var delivery = CreateTestDelivery();

        // Attempt 1 → schedule retry
        delivery.IncrementRetry();
        delivery.ScheduleRetry(DateTimeOffset.UtcNow.AddSeconds(30), "err 1");
        delivery.Status.Should().Be(DeliveryStatus.RetryScheduled);
        delivery.RetryCount.Should().Be(1);

        // Attempt 2 → schedule retry
        delivery.IncrementRetry();
        delivery.ScheduleRetry(DateTimeOffset.UtcNow.AddSeconds(60), "err 2");
        delivery.RetryCount.Should().Be(2);

        // Attempt 3 → delivered
        delivery.IncrementRetry();
        delivery.MarkDelivered();
        delivery.Status.Should().Be(DeliveryStatus.Delivered);
        delivery.RetryCount.Should().Be(3);
        delivery.NextRetryAt.Should().BeNull();
    }

    [Fact]
    public void FullRetryLifecycle_MaxAttemptsExhausted_MarkedFailed()
    {
        var delivery = CreateTestDelivery();

        for (var i = 0; i < 2; i++)
        {
            delivery.IncrementRetry();
            delivery.ScheduleRetry(DateTimeOffset.UtcNow.AddSeconds(30), $"error {i + 1}");
        }

        delivery.IncrementRetry();
        delivery.MarkFailed("Final permanent failure");

        delivery.Status.Should().Be(DeliveryStatus.Failed);
        delivery.RetryCount.Should().Be(3);
    }

    private static NotificationDelivery CreateTestDelivery()
    {
        var notificationId = new NexTraceOne.Notifications.Domain.StronglyTypedIds.NotificationId(Guid.NewGuid());
        return NotificationDelivery.Create(notificationId, DeliveryChannel.Email, "test@example.com");
    }
}
