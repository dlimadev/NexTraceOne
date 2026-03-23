using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.Features.MarkNotificationUnread;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Tests.Application;

public sealed class MarkNotificationUnreadTests
{
    private readonly INotificationStore _store = Substitute.For<INotificationStore>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private MarkNotificationUnread.Handler CreateHandler() => new(_store, _currentUser);

    [Fact]
    public async Task Handle_WithReadNotification_ShouldMarkAsUnread()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUser.Id.Returns(userId.ToString());

        var notification = CreateNotification(userId);
        notification.MarkAsRead(); // Pre-condition: notification is read
        var notificationId = notification.Id.Value;

        _store.GetByIdAsync(Arg.Is<NotificationId>(id => id.Value == notificationId), Arg.Any<CancellationToken>())
            .Returns(notification);

        var handler = CreateHandler();
        var command = new MarkNotificationUnread.Command(notificationId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Unread);
        notification.ReadAt.Should().BeNull();
        await _store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistentNotification_ShouldReturnNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUser.Id.Returns(userId.ToString());

        var notificationId = Guid.NewGuid();
        _store.GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns((Notification?)null);

        var handler = CreateHandler();
        var command = new MarkNotificationUnread.Command(notificationId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("Notification.NotFound");
    }

    [Fact]
    public async Task Handle_WithDifferentUser_ShouldReturnForbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        _currentUser.Id.Returns(userId.ToString());

        var notification = CreateNotification(otherUserId);
        notification.MarkAsRead();
        var notificationId = notification.Id.Value;

        _store.GetByIdAsync(Arg.Is<NotificationId>(id => id.Value == notificationId), Arg.Any<CancellationToken>())
            .Returns(notification);

        var handler = CreateHandler();
        var command = new MarkNotificationUnread.Command(notificationId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
        result.Error.Code.Should().Be("Notification.Forbidden");
        await _store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static Notification CreateNotification(Guid recipientUserId)
        => Notification.Create(
            tenantId: Guid.NewGuid(),
            recipientUserId: recipientUserId,
            eventType: "TestEvent",
            category: NotificationCategory.Incident,
            severity: NotificationSeverity.Warning,
            title: "Test Title",
            message: "Test Message",
            sourceModule: "TestModule",
            actionUrl: "/test/123");
}
