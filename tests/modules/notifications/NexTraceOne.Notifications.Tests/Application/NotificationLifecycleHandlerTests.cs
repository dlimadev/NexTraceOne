using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.Features.AcknowledgeNotification;
using NexTraceOne.Notifications.Application.Features.ArchiveNotification;
using NexTraceOne.Notifications.Application.Features.DismissNotification;
using NexTraceOne.Notifications.Application.Features.SnoozeNotification;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Tests.Application;

/// <summary>
/// Testes de unidade para os handlers de lifecycle de notificação:
/// Acknowledge, Archive, Dismiss, Snooze.
/// </summary>
public sealed class NotificationLifecycleHandlerTests
{
    private readonly INotificationStore _store = Substitute.For<INotificationStore>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly Guid _userId = Guid.NewGuid();

    public NotificationLifecycleHandlerTests()
    {
        _currentUser.Id.Returns(_userId.ToString());
    }

    // ── AcknowledgeNotification ───────────────────────────────────────────

    [Fact]
    public async Task Acknowledge_ValidNotification_ShouldSetStatusAcknowledged()
    {
        var notification = CreateNotification(_userId);
        ArrangeStore(notification);

        var handler = new AcknowledgeNotification.Handler(_store, _currentUser);
        var result = await handler.Handle(
            new AcknowledgeNotification.Command(notification.Id.Value, "Noted"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Acknowledged);
        notification.AcknowledgedAt.Should().NotBeNull();
        notification.AcknowledgeComment.Should().Be("Noted");
        await _store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Acknowledge_NotificationNotFound_ShouldReturnNotFound()
    {
        _store.GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns((Notification?)null);

        var handler = new AcknowledgeNotification.Handler(_store, _currentUser);
        var result = await handler.Handle(
            new AcknowledgeNotification.Command(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Acknowledge_WrongUser_ShouldReturnForbidden()
    {
        var notification = CreateNotification(Guid.NewGuid()); // different user
        ArrangeStore(notification);

        var handler = new AcknowledgeNotification.Handler(_store, _currentUser);
        var result = await handler.Handle(
            new AcknowledgeNotification.Command(notification.Id.Value),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
        await _store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Acknowledge_AlreadyArchived_ShouldReturnConflict()
    {
        var notification = CreateNotification(_userId);
        notification.Archive();
        ArrangeStore(notification);

        var handler = new AcknowledgeNotification.Handler(_store, _currentUser);
        var result = await handler.Handle(
            new AcknowledgeNotification.Command(notification.Id.Value),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        await _store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── ArchiveNotification ───────────────────────────────────────────────

    [Fact]
    public async Task Archive_ValidNotification_ShouldSetStatusArchived()
    {
        var notification = CreateNotification(_userId);
        ArrangeStore(notification);

        var handler = new ArchiveNotification.Handler(_store, _currentUser);
        var result = await handler.Handle(
            new ArchiveNotification.Command(notification.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Archived);
        notification.ArchivedAt.Should().NotBeNull();
        await _store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Archive_NotificationNotFound_ShouldReturnNotFound()
    {
        _store.GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns((Notification?)null);

        var handler = new ArchiveNotification.Handler(_store, _currentUser);
        var result = await handler.Handle(
            new ArchiveNotification.Command(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Archive_WrongUser_ShouldReturnForbidden()
    {
        var notification = CreateNotification(Guid.NewGuid()); // different user
        ArrangeStore(notification);

        var handler = new ArchiveNotification.Handler(_store, _currentUser);
        var result = await handler.Handle(
            new ArchiveNotification.Command(notification.Id.Value),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Archive_AlreadyDismissed_ShouldReturnConflict()
    {
        var notification = CreateNotification(_userId);
        notification.Dismiss();
        ArrangeStore(notification);

        var handler = new ArchiveNotification.Handler(_store, _currentUser);
        var result = await handler.Handle(
            new ArchiveNotification.Command(notification.Id.Value),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        await _store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── DismissNotification ───────────────────────────────────────────────

    [Fact]
    public async Task Dismiss_ValidUnreadNotification_ShouldSetStatusDismissed()
    {
        var notification = CreateNotification(_userId);
        ArrangeStore(notification);

        var handler = new DismissNotification.Handler(_store, _currentUser);
        var result = await handler.Handle(
            new DismissNotification.Command(notification.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        notification.Status.Should().Be(NotificationStatus.Dismissed);
        await _store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Dismiss_NotificationNotFound_ShouldReturnNotFound()
    {
        _store.GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns((Notification?)null);

        var handler = new DismissNotification.Handler(_store, _currentUser);
        var result = await handler.Handle(
            new DismissNotification.Command(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Dismiss_WrongUser_ShouldReturnForbidden()
    {
        var notification = CreateNotification(Guid.NewGuid()); // different user
        ArrangeStore(notification);

        var handler = new DismissNotification.Handler(_store, _currentUser);
        var result = await handler.Handle(
            new DismissNotification.Command(notification.Id.Value),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Dismiss_AlreadyAcknowledged_ShouldReturnConflict()
    {
        var notification = CreateNotification(_userId);
        notification.Acknowledge(_userId);
        ArrangeStore(notification);

        var handler = new DismissNotification.Handler(_store, _currentUser);
        var result = await handler.Handle(
            new DismissNotification.Command(notification.Id.Value),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        await _store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── SnoozeNotification ────────────────────────────────────────────────

    [Fact]
    public async Task Snooze_ValidNotification_ShouldSetSnoozedUntil()
    {
        var notification = CreateNotification(_userId);
        ArrangeStore(notification);
        var until = DateTimeOffset.UtcNow.AddHours(2);

        var handler = new SnoozeNotification.Handler(_store, _currentUser);
        var result = await handler.Handle(
            new SnoozeNotification.Command(notification.Id.Value, until),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        notification.SnoozedUntil.Should().Be(until);
        await _store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Snooze_NotificationNotFound_ShouldReturnNotFound()
    {
        _store.GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns((Notification?)null);

        var handler = new SnoozeNotification.Handler(_store, _currentUser);
        var result = await handler.Handle(
            new SnoozeNotification.Command(Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(1)),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Snooze_WrongUser_ShouldReturnForbidden()
    {
        var notification = CreateNotification(Guid.NewGuid()); // different user
        ArrangeStore(notification);

        var handler = new SnoozeNotification.Handler(_store, _currentUser);
        var result = await handler.Handle(
            new SnoozeNotification.Command(notification.Id.Value, DateTimeOffset.UtcNow.AddHours(1)),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Snooze_PastDate_ShouldReturnValidationError()
    {
        var notification = CreateNotification(_userId);
        ArrangeStore(notification);

        var handler = new SnoozeNotification.Handler(_store, _currentUser);
        var result = await handler.Handle(
            new SnoozeNotification.Command(notification.Id.Value, DateTimeOffset.UtcNow.AddHours(-1)),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        await _store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Snooze_AlreadyDismissed_ShouldReturnConflict()
    {
        var notification = CreateNotification(_userId);
        notification.Dismiss();
        ArrangeStore(notification);

        var handler = new SnoozeNotification.Handler(_store, _currentUser);
        var result = await handler.Handle(
            new SnoozeNotification.Command(notification.Id.Value, DateTimeOffset.UtcNow.AddHours(2)),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private void ArrangeStore(Notification notification)
    {
        _store.GetByIdAsync(
            Arg.Is<NotificationId>(id => id.Value == notification.Id.Value),
            Arg.Any<CancellationToken>())
            .Returns(notification);
    }

    private static Notification CreateNotification(Guid recipientUserId)
        => Notification.Create(
            tenantId: Guid.NewGuid(),
            recipientUserId: recipientUserId,
            eventType: "TestEvent",
            category: NotificationCategory.Change,
            severity: NotificationSeverity.ActionRequired,
            title: "Test Title",
            message: "Test Message",
            sourceModule: "ChangeGovernance",
            requiresAction: true);
}
