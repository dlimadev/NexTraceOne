using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Infrastructure.Intelligence;
using NexTraceOne.Notifications.Infrastructure.Persistence;
using NexTraceOne.Notifications.Infrastructure.Preferences;

namespace NexTraceOne.Notifications.Tests.Intelligence;

/// <summary>
/// Testes de unidade para NotificationSuppressionService.
/// Utiliza EF Core InMemory para validar as regras de supressão contra o contexto real.
///
/// Regras cobertas:
///  - Notificações Critical → nunca suprimidas
///  - Notificações obrigatórias (BreakGlass, ApprovalPending) → nunca suprimidas
///  - Já acknowledged para mesma entidade nos últimos 30 min → suprimida
///  - Snoozed activa para mesma entidade → suprimida
///  - Snooze expirado → não suprimida
///  - Nenhuma correspondência → permitida
/// </summary>
public sealed class NotificationSuppressionServiceTests
{
    // ── Critical severity — always allowed ────────────────────────────────

    [Fact]
    public async Task CriticalNotification_IsNeverSuppressed()
    {
        await using var context = CreateContext();
        var sut = CreateSut(context);

        var request = BuildRequest("IncidentEscalated", "Incident", "Critical", "incident-1");

        var result = await sut.EvaluateAsync(request, Guid.NewGuid());

        result.ShouldSuppress.Should().BeFalse("Critical notifications are never suppressed");
    }

    // ── Mandatory notification types — always allowed ──────────────────────

    [Fact]
    public async Task BreakGlassNotification_IsNeverSuppressed()
    {
        await using var context = CreateContext();
        var sut = CreateSut(context);

        var request = BuildRequest("BreakGlassActivated", "Security", "Warning", "bg-session-1");

        var result = await sut.EvaluateAsync(request, Guid.NewGuid());

        result.ShouldSuppress.Should().BeFalse("BreakGlass is a mandatory event type");
    }

    [Fact]
    public async Task ApprovalPendingNotification_IsNeverSuppressed()
    {
        await using var context = CreateContext();
        var sut = CreateSut(context);

        var request = BuildRequest("ApprovalPending", "Approval", "ActionRequired", "change-123");

        var result = await sut.EvaluateAsync(request, Guid.NewGuid());

        result.ShouldSuppress.Should().BeFalse("ApprovalPending is a mandatory event type");
    }

    // ── No matching notifications — allowed ───────────────────────────────

    [Fact]
    public async Task WhenNoMatchingNotificationsInDatabase_AllowsNotification()
    {
        await using var context = CreateContext();
        var sut = CreateSut(context);

        var request = BuildRequest("IncidentCreated", "Incident", "Warning", "incident-xyz");

        var result = await sut.EvaluateAsync(request, Guid.NewGuid());

        result.ShouldSuppress.Should().BeFalse();
    }

    // ── Acknowledge rule ──────────────────────────────────────────────────

    [Fact]
    public async Task WhenRecentlyAcknowledgedForSameEntity_SuppressesNotification()
    {
        await using var context = CreateContext();

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string eventType = "IncidentCreated";
        const string entityId = "incident-555";

        // Existing notification, acknowledged recently
        var existing = CreateAcknowledgedNotification(tenantId, userId, eventType, entityId);
        await context.Notifications.AddAsync(existing);
        await context.SaveChangesAsync();

        var sut = CreateSut(context);
        var request = BuildRequest(eventType, "Incident", "Warning", entityId, tenantId);

        var result = await sut.EvaluateAsync(request, userId);

        result.ShouldSuppress.Should().BeTrue();
        result.Reason.Should().Contain("acknowledged");
    }

    [Fact]
    public async Task WhenAcknowledgedForDifferentEntity_AllowsNotification()
    {
        await using var context = CreateContext();

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string eventType = "IncidentCreated";

        // Acknowledged notification for entity "A"
        var existing = CreateAcknowledgedNotification(tenantId, userId, eventType, "incident-A");
        await context.Notifications.AddAsync(existing);
        await context.SaveChangesAsync();

        var sut = CreateSut(context);

        // New notification for entity "B" — different entity, should not be suppressed
        var request = BuildRequest(eventType, "Incident", "Warning", "incident-B", tenantId);

        var result = await sut.EvaluateAsync(request, userId);

        result.ShouldSuppress.Should().BeFalse("acknowledged notification for a different entity should not suppress the new one");
    }

    // ── Snooze rule ───────────────────────────────────────────────────────

    [Fact]
    public async Task WhenActiveSnoozedForSameEntity_SuppressesNotification()
    {
        await using var context = CreateContext();

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string eventType = "PostChangeVerificationFailed";
        const string entityId = "release-789";

        // Existing notification with active snooze (snoozed until 2 hours from now)
        var existing = CreateSnoozedNotification(tenantId, userId, eventType, entityId,
            snoozedUntil: DateTimeOffset.UtcNow.AddHours(2));
        await context.Notifications.AddAsync(existing);
        await context.SaveChangesAsync();

        var sut = CreateSut(context);
        var request = BuildRequest(eventType, "Change", "Warning", entityId, tenantId);

        var result = await sut.EvaluateAsync(request, userId);

        result.ShouldSuppress.Should().BeTrue();
        result.Reason.Should().Contain("snooze");
    }

    [Fact]
    public async Task WhenSnoozeExpired_AllowsNotification()
    {
        await using var context = CreateContext();

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string eventType = "PostChangeVerificationFailed";
        const string entityId = "release-expired";

        // Existing notification with an expired snooze (snoozed until 1 hour ago)
        var existing = CreateSnoozedNotification(tenantId, userId, eventType, entityId,
            snoozedUntil: DateTimeOffset.UtcNow.AddHours(-1));
        await context.Notifications.AddAsync(existing);
        await context.SaveChangesAsync();

        var sut = CreateSut(context);
        var request = BuildRequest(eventType, "Change", "Warning", entityId, tenantId);

        var result = await sut.EvaluateAsync(request, userId);

        result.ShouldSuppress.Should().BeFalse("expired snooze should not suppress new notifications");
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static NotificationSuppressionService CreateSut(NotificationsDbContext context)
    {
        var configResolution = Substitute.For<IConfigurationResolutionService>();
        configResolution.ResolveEffectiveValueAsync(
                Arg.Any<string>(),
                Arg.Any<ConfigurationScope>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns((NexTraceOne.Configuration.Contracts.DTOs.EffectiveConfigurationDto?)null);
        return new NotificationSuppressionService(context, new MandatoryNotificationPolicy(), configResolution);
    }

    private static NotificationsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseInMemoryDatabase($"ntf-suppress-tests-{Guid.NewGuid():N}")
            .Options;

        var now = DateTimeOffset.UtcNow;
        return new NotificationsDbContext(
            options,
            new TestCurrentTenant(),
            new TestCurrentUser(),
            new TestDateTimeProvider(now));
    }

    private static NotificationRequest BuildRequest(
        string eventType,
        string category,
        string severity,
        string? sourceEntityId,
        Guid? tenantId = null) => new()
    {
        EventType = eventType,
        Category = category,
        Severity = severity,
        Title = "Test notification",
        Message = "Test message",
        SourceModule = "TestModule",
        SourceEntityType = "TestEntity",
        SourceEntityId = sourceEntityId,
        TenantId = tenantId ?? Guid.NewGuid(),
        RecipientUserIds = [],
    };

    private static Notification CreateAcknowledgedNotification(
        Guid tenantId, Guid userId, string eventType, string entityId)
    {
        var n = Notification.Create(
            tenantId, userId, eventType,
            NotificationCategory.Incident, NotificationSeverity.Warning,
            "Test", "Test message", "TestModule",
            sourceEntityType: "Incident",
            sourceEntityId: entityId);

        n.Acknowledge(userId, "Investigating");
        return n;
    }

    private static Notification CreateSnoozedNotification(
        Guid tenantId, Guid userId, string eventType, string entityId,
        DateTimeOffset snoozedUntil)
    {
        var n = Notification.Create(
            tenantId, userId, eventType,
            NotificationCategory.Change, NotificationSeverity.Warning,
            "Test", "Test message", "TestModule",
            sourceEntityType: "Release",
            sourceEntityId: entityId);

        n.Snooze(snoozedUntil, userId);
        return n;
    }

    private sealed class TestCurrentTenant : ICurrentTenant
    {
        public Guid Id => Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");
        public string Slug => "ntf-suppress-tests";
        public string Name => "Suppression Tests Tenant";
        public bool IsActive => true;
        public bool HasCapability(string capability) => true;
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public string Id => "ntf-suppress-tests-user";
        public string Name => "Suppression Tests";
        public string Email => "suppress.tests@nextraceone.local";
        public string? Persona { get; } = null;
        public bool IsAuthenticated => true;
        public bool HasPermission(string permission) => true;
    }

    private sealed class TestDateTimeProvider(DateTimeOffset now) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
        public DateOnly UtcToday => DateOnly.FromDateTime(now.UtcDateTime);
    }
}
