using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Infrastructure.Engine;
using NexTraceOne.Notifications.Infrastructure.Persistence;

namespace NexTraceOne.Notifications.Tests.Engine;

/// <summary>
/// Testes de unidade para NotificationDeduplicationService.
/// Utiliza EF Core InMemory para validar consultas reais ao contexto.
/// Nota: o serviço usa DateTimeOffset.UtcNow internamente (não IDateTimeProvider),
/// portanto as notificações "recentes" são criadas com createdAt próximo de DateTimeOffset.UtcNow.
/// </summary>
public sealed class NotificationDeduplicationServiceTests
{
    // ── Pure logic paths (sem acesso ao DB) ───────────────────────────────

    [Fact]
    public async Task WhenWindowIsZero_ReturnsFalseImmediately()
    {
        await using var context = CreateContext();
        var sut = new NotificationDeduplicationService(context);

        var result = await sut.IsDuplicateAsync(
            Guid.NewGuid(), Guid.NewGuid(), "IncidentCreated",
            sourceEntityId: "entity-1",
            windowMinutes: 0);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task WhenWindowIsNegative_ReturnsFalseImmediately()
    {
        await using var context = CreateContext();
        var sut = new NotificationDeduplicationService(context);

        var result = await sut.IsDuplicateAsync(
            Guid.NewGuid(), Guid.NewGuid(), "IncidentCreated",
            sourceEntityId: null,
            windowMinutes: -10);

        result.Should().BeFalse();
    }

    // ── Database-backed paths ──────────────────────────────────────────────

    [Fact]
    public async Task WhenNoMatchingNotifications_ReturnsFalse()
    {
        await using var context = CreateContext();
        var sut = new NotificationDeduplicationService(context);

        var result = await sut.IsDuplicateAsync(
            Guid.NewGuid(), Guid.NewGuid(), "IncidentCreated", "entity-1");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task WhenRecentMatchingNotificationExists_ReturnsTrue()
    {
        await using var context = CreateContext();

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string eventType = "IncidentCreated";
        const string entityId = "incident-abc";

        // Notification created just now — within any reasonable window
        var notification = CreateNotification(tenantId, userId, eventType, entityId);
        await context.Notifications.AddAsync(notification);
        await context.SaveChangesAsync();

        var sut = new NotificationDeduplicationService(context);

        var result = await sut.IsDuplicateAsync(
            tenantId, userId, eventType, entityId, windowMinutes: 5);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task WhenMatchingNotificationIsOlderThanWindow_ReturnsFalse()
    {
        await using var context = CreateContext();

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string eventType = "IncidentCreated";
        const string entityId = "incident-old";

        // Notification created 2h ago — outside a 5-minute window
        var notification = CreateNotification(tenantId, userId, eventType, entityId,
            createdAtOverride: DateTimeOffset.UtcNow.AddHours(-2));
        await context.Notifications.AddAsync(notification);
        await context.SaveChangesAsync();

        var sut = new NotificationDeduplicationService(context);

        var result = await sut.IsDuplicateAsync(
            tenantId, userId, eventType, entityId, windowMinutes: 5);

        result.Should().BeFalse("notification older than the window is not a duplicate");
    }

    [Fact]
    public async Task WhenNotificationIsDismissed_ReturnsFalse()
    {
        await using var context = CreateContext();

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string eventType = "ContractBrokenChange";
        const string entityId = "contract-xyz";

        var notification = CreateNotification(tenantId, userId, eventType, entityId);
        notification.Dismiss();

        await context.Notifications.AddAsync(notification);
        await context.SaveChangesAsync();

        var sut = new NotificationDeduplicationService(context);

        var result = await sut.IsDuplicateAsync(
            tenantId, userId, eventType, entityId);

        result.Should().BeFalse("dismissed notifications are excluded from deduplication window");
    }

    [Fact]
    public async Task WhenNotificationIsArchived_ReturnsFalse()
    {
        await using var context = CreateContext();

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string eventType = "ComplianceCheckFailed";
        const string entityId = "policy-99";

        var notification = CreateNotification(tenantId, userId, eventType, entityId);
        notification.Archive();

        await context.Notifications.AddAsync(notification);
        await context.SaveChangesAsync();

        var sut = new NotificationDeduplicationService(context);

        var result = await sut.IsDuplicateAsync(
            tenantId, userId, eventType, entityId);

        result.Should().BeFalse("archived notifications are excluded from deduplication window");
    }

    [Fact]
    public async Task WhenSourceEntityIdDiffers_ReturnsFalse()
    {
        await using var context = CreateContext();

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string eventType = "ChangeConfidenceScored";

        // Existing notification for entityId "release-A"
        var notification = CreateNotification(tenantId, userId, eventType, "release-A");
        await context.Notifications.AddAsync(notification);
        await context.SaveChangesAsync();

        var sut = new NotificationDeduplicationService(context);

        // Check duplication for "release-B" → different entity → not a duplicate
        var result = await sut.IsDuplicateAsync(
            tenantId, userId, eventType, "release-B");

        result.Should().BeFalse("different source entity ids are not considered duplicates");
    }

    [Fact]
    public async Task WhenSourceEntityIdIsNull_MatchesAnyNotificationForEventType()
    {
        await using var context = CreateContext();

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string eventType = "PlatformAlert";

        var notification = CreateNotification(tenantId, userId, eventType, entityId: null);
        await context.Notifications.AddAsync(notification);
        await context.SaveChangesAsync();

        var sut = new NotificationDeduplicationService(context);

        // Null sourceEntityId → no entity filter → matches any recent notification for the event type
        var result = await sut.IsDuplicateAsync(
            tenantId, userId, eventType, sourceEntityId: null);

        result.Should().BeTrue("when sourceEntityId is null the service omits the entity filter and matches any recent notification");
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static NotificationsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseInMemoryDatabase($"ntf-dedup-tests-{Guid.NewGuid():N}")
            .Options;

        var now = DateTimeOffset.UtcNow;
        return new NotificationsDbContext(
            options,
            new TestCurrentTenant(),
            new TestCurrentUser(),
            new TestDateTimeProvider(now));
    }

    /// <summary>
    /// Cria uma notificação de teste. Quando <paramref name="createdAtOverride"/> é fornecido,
    /// usa reflexão para substituir o valor imutável de CreatedAt (definido no construtor do domínio).
    /// </summary>
    private static Notification CreateNotification(
        Guid tenantId,
        Guid recipientUserId,
        string eventType,
        string? entityId,
        DateTimeOffset? createdAtOverride = null)
    {
        var notification = Notification.Create(
            tenantId,
            recipientUserId,
            eventType,
            NotificationCategory.Incident,
            NotificationSeverity.Warning,
            title: "Test notification",
            message: "Test message",
            sourceModule: "TestModule",
            sourceEntityType: entityId is not null ? "Incident" : null,
            sourceEntityId: entityId);

        if (createdAtOverride.HasValue)
        {
            // CreatedAt is private set — use backing field via reflection to override for tests
            var prop = typeof(Notification)
                .GetProperty("CreatedAt",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            prop?.SetValue(notification, createdAtOverride.Value);
        }

        return notification;
    }

    private sealed class TestCurrentTenant : ICurrentTenant
    {
        public Guid Id => Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        public string Slug => "ntf-dedup-tests";
        public string Name => "Dedup Tests Tenant";
        public bool IsActive => true;
        public bool HasCapability(string capability) => true;
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public string Id => "ntf-dedup-tests-user";
        public string Name => "Dedup Tests";
        public string Email => "dedup.tests@nextraceone.local";
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
