using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.Features.ListNotifications;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Tests.Application;

public sealed class ListNotificationsTests
{
    private readonly INotificationStore _store = Substitute.For<INotificationStore>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private ListNotifications.Handler CreateHandler() => new(_store, _currentUser);

    [Fact]
    public async Task Handle_WithValidUser_ShouldReturnNotifications()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUser.Id.Returns(userId.ToString());

        var notifications = new List<Notification>
        {
            CreateNotification(userId),
            CreateNotification(userId),
        };

        _store.ListAsync(userId, null, null, null, 0, 21, Arg.Any<CancellationToken>())
            .Returns(notifications.AsReadOnly());

        var handler = CreateHandler();
        var query = new ListNotifications.Query(Status: null, Category: null, Severity: null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ShouldPassFilterToStore()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUser.Id.Returns(userId.ToString());

        _store.ListAsync(userId, NotificationStatus.Unread, null, null, 0, 21, Arg.Any<CancellationToken>())
            .Returns(new List<Notification>().AsReadOnly());

        var handler = CreateHandler();
        var query = new ListNotifications.Query(Status: "Unread", Category: null, Severity: null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();

        await _store.Received(1).ListAsync(
            userId, NotificationStatus.Unread, null, null, 0, 21, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNoResults_ShouldReturnEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUser.Id.Returns(userId.ToString());

        _store.ListAsync(userId, null, null, null, 0, 21, Arg.Any<CancellationToken>())
            .Returns(new List<Notification>().AsReadOnly());

        var handler = CreateHandler();
        var query = new ListNotifications.Query(Status: null, Category: null, Severity: null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnError()
    {
        // Arrange
        _currentUser.Id.Returns("not-a-guid");

        var handler = CreateHandler();
        var query = new ListNotifications.Query(Status: null, Category: null, Severity: null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
        result.Error.Code.Should().Be("Notification.InvalidUserId");
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
