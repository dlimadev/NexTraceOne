using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Infrastructure.Intelligence;

namespace NexTraceOne.Notifications.Tests.Intelligence;

/// <summary>
/// Testes para o NotificationEscalationService da Fase 6.
/// Valida critérios de escalação: severidade, tempo, estado e snooze.
/// </summary>
public sealed class NotificationEscalationServiceTests
{
    private readonly NotificationEscalationService _service;

    public NotificationEscalationServiceTests()
    {
        _service = new NotificationEscalationService(
            NullLoggerFactory.Instance.CreateLogger<NotificationEscalationService>());
    }

    [Fact]
    public void ShouldEscalate_CriticalUnread_OlderThan30Min_ReturnsTrue()
    {
        var notification = CreateNotification(
            NotificationSeverity.Critical,
            createdMinutesAgo: 31);

        _service.ShouldEscalate(notification).Should().BeTrue();
    }

    [Fact]
    public void ShouldEscalate_CriticalUnread_Within30Min_ReturnsFalse()
    {
        var notification = CreateNotification(
            NotificationSeverity.Critical,
            createdMinutesAgo: 15);

        _service.ShouldEscalate(notification).Should().BeFalse();
    }

    [Fact]
    public void ShouldEscalate_ActionRequiredUnread_OlderThan2Hours_ReturnsTrue()
    {
        var notification = CreateNotification(
            NotificationSeverity.ActionRequired,
            createdMinutesAgo: 121,
            requiresAction: true);

        _service.ShouldEscalate(notification).Should().BeTrue();
    }

    [Fact]
    public void ShouldEscalate_ActionRequiredUnread_Within2Hours_ReturnsFalse()
    {
        var notification = CreateNotification(
            NotificationSeverity.ActionRequired,
            createdMinutesAgo: 90,
            requiresAction: true);

        _service.ShouldEscalate(notification).Should().BeFalse();
    }

    [Fact]
    public void ShouldEscalate_InfoSeverity_ReturnsFalse()
    {
        var notification = CreateNotification(
            NotificationSeverity.Info,
            createdMinutesAgo: 120);

        _service.ShouldEscalate(notification).Should().BeFalse();
    }

    [Fact]
    public void ShouldEscalate_WarningSeverity_ReturnsFalse()
    {
        var notification = CreateNotification(
            NotificationSeverity.Warning,
            createdMinutesAgo: 120);

        _service.ShouldEscalate(notification).Should().BeFalse();
    }

    [Fact]
    public void ShouldEscalate_AlreadyEscalated_ReturnsFalse()
    {
        var notification = CreateNotification(
            NotificationSeverity.Critical,
            createdMinutesAgo: 60);
        notification.MarkAsEscalated();

        _service.ShouldEscalate(notification).Should().BeFalse();
    }

    [Fact]
    public void ShouldEscalate_Acknowledged_ReturnsFalse()
    {
        var notification = CreateNotification(
            NotificationSeverity.Critical,
            createdMinutesAgo: 60);
        notification.Acknowledge();

        _service.ShouldEscalate(notification).Should().BeFalse();
    }

    [Fact]
    public void ShouldEscalate_Archived_ReturnsFalse()
    {
        var notification = CreateNotification(
            NotificationSeverity.Critical,
            createdMinutesAgo: 60);
        notification.Archive();

        _service.ShouldEscalate(notification).Should().BeFalse();
    }

    [Fact]
    public void ShouldEscalate_Snoozed_ReturnsFalse()
    {
        var notification = CreateNotification(
            NotificationSeverity.Critical,
            createdMinutesAgo: 60);
        notification.Snooze(DateTimeOffset.UtcNow.AddHours(2), Guid.NewGuid());

        _service.ShouldEscalate(notification).Should().BeFalse();
    }

    [Fact]
    public async Task EscalateAsync_ShouldMarkNotificationAsEscalated()
    {
        var notification = CreateNotification(
            NotificationSeverity.Critical,
            createdMinutesAgo: 60);

        await _service.EscalateAsync(notification);

        notification.IsEscalated.Should().BeTrue();
        notification.EscalatedAt.Should().NotBeNull();
    }

    /// <summary>
    /// Creates a notification with a backdated CreatedAt for escalation testing.
    /// Uses reflection to set the private CreatedAt since the entity has no public setter.
    /// </summary>
    private static Notification CreateNotification(
        NotificationSeverity severity,
        int createdMinutesAgo,
        bool requiresAction = false)
    {
        var notification = Notification.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            "TestEvent", NotificationCategory.Incident,
            severity,
            "Test", "Test message",
            "TestModule",
            requiresAction: requiresAction);

        // Backdate CreatedAt using reflection (entity has private setter)
        var createdAtProp = typeof(Notification).GetProperty("CreatedAt")!;
        createdAtProp.SetValue(notification, DateTimeOffset.UtcNow.AddMinutes(-createdMinutesAgo));

        return notification;
    }
}
