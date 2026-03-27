using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.AuditCompliance.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Infrastructure.Governance;

namespace NexTraceOne.Notifications.Tests.Governance;

/// <summary>
/// Testes para o NotificationAuditService da Fase 7.
/// P7.3: valida que o serviço chama IAuditModule.RecordEventAsync para cada tipo de acção auditável.
/// </summary>
public sealed class NotificationAuditServiceTests
{
    private readonly IAuditModule _auditModule = Substitute.For<IAuditModule>();
    private readonly NotificationAuditService _service;

    public NotificationAuditServiceTests()
    {
        _service = new NotificationAuditService(
            _auditModule,
            NullLoggerFactory.Instance.CreateLogger<NotificationAuditService>());
    }

    [Fact]
    public async Task RecordAsync_CriticalNotificationGenerated_CallsAuditModule()
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

        await _auditModule.Received(1).RecordEventAsync(
            "notifications",
            NotificationAuditActions.CriticalNotificationGenerated,
            entry.ResourceId,
            "Notification",
            "system",
            entry.TenantId,
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordAsync_NotificationAcknowledged_PassesPerformedBy()
    {
        var userId = Guid.NewGuid();
        var entry = new NotificationAuditEntry
        {
            TenantId = Guid.NewGuid(),
            ActionType = NotificationAuditActions.NotificationAcknowledged,
            ResourceId = Guid.NewGuid().ToString(),
            ResourceType = "Notification",
            PerformedBy = userId,
            Description = "User acknowledged critical incident notification"
        };

        await _service.RecordAsync(entry);

        await _auditModule.Received(1).RecordEventAsync(
            "notifications",
            NotificationAuditActions.NotificationAcknowledged,
            entry.ResourceId,
            "Notification",
            userId.ToString(),
            entry.TenantId,
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordAsync_NotificationEscalated_ShouldCallAuditModule()
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

        await _auditModule.Received(1).RecordEventAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordAsync_PreferencesChanged_ShouldCallAuditModule()
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

        await _auditModule.Received(1).RecordEventAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordAsync_NullEntry_ShouldThrow()
    {
        var act = () => _service.RecordAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RecordAsync_AuditModuleThrows_ShouldNotPropagate()
    {
        // Best-effort: if IAuditModule throws, the caller is not affected
        _auditModule
            .RecordEventAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>(),
                Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Audit module unavailable")));

        var entry = new NotificationAuditEntry
        {
            TenantId = Guid.NewGuid(),
            ActionType = NotificationAuditActions.CriticalNotificationGenerated,
            ResourceId = Guid.NewGuid().ToString(),
            ResourceType = "Notification"
        };

        // Should NOT throw — best-effort semantics
        await _service.RecordAsync(entry);
    }

    [Theory]
    [InlineData(NotificationAuditActions.NotificationGenerated)]
    [InlineData(NotificationAuditActions.CriticalNotificationGenerated)]
    [InlineData(NotificationAuditActions.CriticalNotificationDelivered)]
    [InlineData(NotificationAuditActions.CriticalNotificationFailed)]
    [InlineData(NotificationAuditActions.NotificationDelivered)]
    [InlineData(NotificationAuditActions.NotificationDeliveryFailed)]
    [InlineData(NotificationAuditActions.NotificationDeliveryRetryScheduled)]
    [InlineData(NotificationAuditActions.NotificationAcknowledged)]
    [InlineData(NotificationAuditActions.NotificationSnoozed)]
    [InlineData(NotificationAuditActions.NotificationEscalated)]
    [InlineData(NotificationAuditActions.IncidentCreatedFromNotification)]
    [InlineData(NotificationAuditActions.PreferencesChanged)]
    [InlineData(NotificationAuditActions.NotificationSuppressed)]
    public async Task RecordAsync_AllActionTypes_CallAuditModule(string actionType)
    {
        var entry = new NotificationAuditEntry
        {
            TenantId = Guid.NewGuid(),
            ActionType = actionType,
            ResourceId = Guid.NewGuid().ToString(),
            ResourceType = "Notification"
        };

        await _service.RecordAsync(entry);

        await _auditModule.Received(1).RecordEventAsync(
            Arg.Any<string>(), actionType, Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }
}
