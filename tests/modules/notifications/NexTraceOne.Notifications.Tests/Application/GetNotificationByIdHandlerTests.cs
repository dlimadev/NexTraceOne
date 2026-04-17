using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.Features.GetNotificationById;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Tests.Application;

/// <summary>
/// Testes para GetNotificationById — detalhe completo de uma notificação por ID.
/// Cobre: happy path, not found, acesso negado a outro utilizador, utilizador inválido.
/// </summary>
public sealed class GetNotificationByIdHandlerTests
{
    private readonly INotificationStore _store = Substitute.For<INotificationStore>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetNotificationById.Handler CreateHandler() => new(_store, _currentUser);

    private static Notification CreateNotification(Guid recipientUserId, bool requiresAction = false)
        => Notification.Create(
            tenantId: Guid.NewGuid(),
            recipientUserId: recipientUserId,
            eventType: "IncidentCreated",
            category: NotificationCategory.Incident,
            severity: NotificationSeverity.Critical,
            title: "Critical Incident",
            message: "A critical incident was created affecting the payment service.",
            sourceModule: "OperationalIntelligence",
            requiresAction: requiresAction,
            actionUrl: "/operations/incidents/inc-123");

    [Fact]
    public async Task Handle_ValidRequest_ReturnsFullNotificationDetail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUser.Id.Returns(userId.ToString());

        var notification = CreateNotification(userId, requiresAction: true);
        _store.GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns(notification);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetNotificationById.Query(notification.Id.Value), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Notification.Id.Should().Be(notification.Id.Value);
        result.Value.Notification.Title.Should().Be("Critical Incident");
        result.Value.Notification.Message.Should().Contain("payment service");
        result.Value.Notification.Category.Should().Be("Incident");
        result.Value.Notification.Severity.Should().Be("Critical");
        result.Value.Notification.EventType.Should().Be("IncidentCreated");
        result.Value.Notification.SourceModule.Should().Be("OperationalIntelligence");
        result.Value.Notification.RequiresAction.Should().BeTrue();
        result.Value.Notification.ActionUrl.Should().Be("/operations/incidents/inc-123");
    }

    [Fact]
    public async Task Handle_NotificationNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUser.Id.Returns(userId.ToString());

        _store.GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns((Notification?)null);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetNotificationById.Query(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Notification.NotFound");
    }

    [Fact]
    public async Task Handle_DifferentUserOwnsNotification_ReturnsForbidden()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid(); // different user
        _currentUser.Id.Returns(callerId.ToString());

        var notification = CreateNotification(ownerId);
        _store.GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns(notification);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetNotificationById.Query(notification.Id.Value), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Notification.AccessDenied");
    }

    [Fact]
    public async Task Handle_InvalidCurrentUserId_ReturnsUnauthorized()
    {
        // Arrange
        _currentUser.Id.Returns("not-a-guid");

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetNotificationById.Query(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Notification.InvalidUserId");
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsMappedStatusFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUser.Id.Returns(userId.ToString());

        var notification = CreateNotification(userId);
        // Mark as read to verify readAt is mapped
        notification.MarkAsRead();

        _store.GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns(notification);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetNotificationById.Query(notification.Id.Value), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Notification.Status.Should().Be("Read");
        result.Value.Notification.ReadAt.Should().NotBeNull();
        result.Value.Notification.AcknowledgedAt.Should().BeNull();
        result.Value.Notification.ArchivedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_AcknowledgedNotification_ReturnsAcknowledgedAt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUser.Id.Returns(userId.ToString());

        var notification = CreateNotification(userId, requiresAction: true);
        notification.Acknowledge();

        _store.GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns(notification);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetNotificationById.Query(notification.Id.Value), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Notification.AcknowledgedAt.Should().NotBeNull();
        result.Value.Notification.Status.Should().Be("Acknowledged");
    }
}
