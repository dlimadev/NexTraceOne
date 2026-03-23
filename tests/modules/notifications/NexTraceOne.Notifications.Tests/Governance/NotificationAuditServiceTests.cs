using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Infrastructure.Governance;

namespace NexTraceOne.Notifications.Tests.Governance;

/// <summary>
/// Testes para o NotificationAuditService da Fase 7.
/// Valida registo de eventos auditáveis sem falha.
/// </summary>
public sealed class NotificationAuditServiceTests
{
    private readonly NotificationAuditService _service;

    public NotificationAuditServiceTests()
    {
        _service = new NotificationAuditService(
            NullLoggerFactory.Instance.CreateLogger<NotificationAuditService>());
    }

    [Fact]
    public async Task RecordAsync_CriticalNotificationGenerated_ShouldNotThrow()
    {
        var entry = new NotificationAuditEntry
        {
            TenantId = Guid.NewGuid(),
            ActionType = NotificationAuditActions.CriticalNotificationGenerated,
            ResourceId = Guid.NewGuid().ToString(),
            ResourceType = "Notification",
            PerformedBy = null,
            Description = "Critical notification generated for IncidentCreated"
        };

        await _service.RecordAsync(entry);
        // Should complete without exception
    }

    [Fact]
    public async Task RecordAsync_NotificationAcknowledged_ShouldNotThrow()
    {
        var entry = new NotificationAuditEntry
        {
            TenantId = Guid.NewGuid(),
            ActionType = NotificationAuditActions.NotificationAcknowledged,
            ResourceId = Guid.NewGuid().ToString(),
            ResourceType = "Notification",
            PerformedBy = Guid.NewGuid(),
            Description = "User acknowledged critical incident notification"
        };

        await _service.RecordAsync(entry);
    }

    [Fact]
    public async Task RecordAsync_NotificationEscalated_ShouldNotThrow()
    {
        var entry = new NotificationAuditEntry
        {
            TenantId = Guid.NewGuid(),
            ActionType = NotificationAuditActions.NotificationEscalated,
            ResourceId = Guid.NewGuid().ToString(),
            ResourceType = "Notification",
            Description = "Notification escalated after 30 minutes without acknowledgement"
        };

        await _service.RecordAsync(entry);
    }

    [Fact]
    public async Task RecordAsync_PreferencesChanged_ShouldNotThrow()
    {
        var entry = new NotificationAuditEntry
        {
            TenantId = Guid.NewGuid(),
            ActionType = NotificationAuditActions.PreferencesChanged,
            ResourceId = Guid.NewGuid().ToString(),
            ResourceType = "NotificationPreference",
            PerformedBy = Guid.NewGuid(),
            Description = "User changed email notification preference"
        };

        await _service.RecordAsync(entry);
    }

    [Fact]
    public async Task RecordAsync_NullEntry_ShouldThrow()
    {
        var act = () => _service.RecordAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData(NotificationAuditActions.CriticalNotificationGenerated)]
    [InlineData(NotificationAuditActions.CriticalNotificationDelivered)]
    [InlineData(NotificationAuditActions.CriticalNotificationFailed)]
    [InlineData(NotificationAuditActions.NotificationAcknowledged)]
    [InlineData(NotificationAuditActions.NotificationSnoozed)]
    [InlineData(NotificationAuditActions.NotificationEscalated)]
    [InlineData(NotificationAuditActions.IncidentCreatedFromNotification)]
    [InlineData(NotificationAuditActions.PreferencesChanged)]
    [InlineData(NotificationAuditActions.NotificationSuppressed)]
    public async Task RecordAsync_AllActionTypes_ShouldNotThrow(string actionType)
    {
        var entry = new NotificationAuditEntry
        {
            TenantId = Guid.NewGuid(),
            ActionType = actionType,
            ResourceId = Guid.NewGuid().ToString(),
            ResourceType = "Notification"
        };

        await _service.RecordAsync(entry);
    }
}
