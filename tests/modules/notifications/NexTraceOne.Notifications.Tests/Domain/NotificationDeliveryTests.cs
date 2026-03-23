using FluentAssertions;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Tests.Domain;

/// <summary>
/// Testes de unidade para a entidade NotificationDelivery.
/// Valida criação, transições de estado e retry.
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
    }

    [Fact]
    public void MarkFailed_ShouldTransitionToFailed()
    {
        var delivery = CreateTestDelivery();

        delivery.MarkFailed("SMTP connection refused");

        delivery.Status.Should().Be(DeliveryStatus.Failed);
        delivery.FailedAt.Should().NotBeNull();
        delivery.ErrorMessage.Should().Be("SMTP connection refused");
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
    }

    [Fact]
    public void IncrementRetry_ShouldIncrementCountAndResetToPending()
    {
        var delivery = CreateTestDelivery();
        delivery.MarkFailed("Timeout");

        delivery.IncrementRetry();

        delivery.RetryCount.Should().Be(1);
        delivery.Status.Should().Be(DeliveryStatus.Pending);
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

    private static NotificationDelivery CreateTestDelivery()
    {
        var notificationId = new NexTraceOne.Notifications.Domain.StronglyTypedIds.NotificationId(Guid.NewGuid());
        return NotificationDelivery.Create(notificationId, DeliveryChannel.Email, "test@example.com");
    }
}
