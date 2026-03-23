using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.Features.MarkAllNotificationsRead;

namespace NexTraceOne.Notifications.Tests.Application;

public sealed class MarkAllNotificationsReadTests
{
    private readonly INotificationStore _store = Substitute.For<INotificationStore>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private MarkAllNotificationsRead.Handler CreateHandler() => new(_store, _currentUser);

    [Fact]
    public async Task Handle_WithValidUser_ShouldMarkAllAsRead()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUser.Id.Returns(userId.ToString());

        var handler = CreateHandler();
        var command = new MarkAllNotificationsRead.Command();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _store.Received(1).MarkAllAsReadAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnError()
    {
        // Arrange
        _currentUser.Id.Returns("not-a-guid");

        var handler = CreateHandler();
        var command = new MarkAllNotificationsRead.Command();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
        result.Error.Code.Should().Be("Notification.InvalidUserId");
        await _store.DidNotReceive().MarkAllAsReadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
