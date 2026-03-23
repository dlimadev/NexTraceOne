using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.Features.GetUnreadCount;

namespace NexTraceOne.Notifications.Tests.Application;

public sealed class GetUnreadCountTests
{
    private readonly INotificationStore _store = Substitute.For<INotificationStore>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetUnreadCount.Handler CreateHandler() => new(_store, _currentUser);

    [Fact]
    public async Task Handle_WithValidUser_ShouldReturnCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUser.Id.Returns(userId.ToString());
        _store.CountUnreadAsync(userId, Arg.Any<CancellationToken>()).Returns(5);

        var handler = CreateHandler();
        var query = new GetUnreadCount.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UnreadCount.Should().Be(5);
    }

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnError()
    {
        // Arrange
        _currentUser.Id.Returns("not-a-guid");

        var handler = CreateHandler();
        var query = new GetUnreadCount.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
        result.Error.Code.Should().Be("Notification.InvalidUserId");
    }
}
