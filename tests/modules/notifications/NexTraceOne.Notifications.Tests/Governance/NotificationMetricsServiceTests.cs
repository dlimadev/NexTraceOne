using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Infrastructure.Governance;
using NexTraceOne.Notifications.Infrastructure.Persistence;

namespace NexTraceOne.Notifications.Tests.Governance;

/// <summary>
/// Testes de integração (EF InMemory) para NotificationMetricsService.
/// Valida as três dimensões de métricas: plataforma, interação e qualidade.
/// </summary>
public sealed class NotificationMetricsServiceTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId1 = Guid.NewGuid();
    private readonly Guid _userId2 = Guid.NewGuid();

    private static readonly DateTimeOffset BaseTime = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    // ── GetPlatformMetrics ───────────────────────────────────────────────────

    [Fact]
    public async Task GetPlatformMetrics_EmptyDb_ReturnsAllZeros()
    {
        await using var context = CreateContext();
        var svc = CreateService(context);

        var result = await svc.GetPlatformMetricsAsync(
            _tenantId,
            BaseTime.AddDays(-7),
            BaseTime);

        result.TotalGenerated.Should().Be(0);
        result.ByCategory.Should().BeEmpty();
        result.BySeverity.Should().BeEmpty();
        result.BySourceModule.Should().BeEmpty();
        result.DeliveriesByChannel.Should().BeEmpty();
        result.TotalDelivered.Should().Be(0);
        result.TotalFailed.Should().Be(0);
        result.TotalPending.Should().Be(0);
        result.TotalSkipped.Should().Be(0);
    }

    [Fact]
    public async Task GetPlatformMetrics_WithNotifications_ReturnsCorrectCategoryAndSeverityBreakdown()
    {
        await using var context = CreateContext();
        var n1 = MakeNotification(_tenantId, _userId1, "IncidentCreated",
            NotificationCategory.Incident, NotificationSeverity.Critical, BaseTime.AddHours(-1));
        var n2 = MakeNotification(_tenantId, _userId1, "IncidentCreated",
            NotificationCategory.Incident, NotificationSeverity.Critical, BaseTime.AddHours(-2));
        var n3 = MakeNotification(_tenantId, _userId2, "DeploymentCompleted",
            NotificationCategory.Change, NotificationSeverity.Info, BaseTime.AddHours(-3));

        context.Notifications.AddRange(n1, n2, n3);
        await context.SaveChangesAsync();

        var svc = CreateService(context);
        var result = await svc.GetPlatformMetricsAsync(
            _tenantId,
            BaseTime.AddDays(-1),
            BaseTime);

        result.TotalGenerated.Should().Be(3);
        result.ByCategory.Should().ContainKey("Incident").WhoseValue.Should().Be(2);
        result.ByCategory.Should().ContainKey("Change").WhoseValue.Should().Be(1);
        result.BySeverity.Should().ContainKey("Critical").WhoseValue.Should().Be(2);
        result.BySeverity.Should().ContainKey("Info").WhoseValue.Should().Be(1);
        result.BySourceModule.Should().ContainKey("TestModule");
    }

    [Fact]
    public async Task GetPlatformMetrics_FiltersOutOfRangeNotifications()
    {
        await using var context = CreateContext();
        // In-range notification
        var inRange = MakeNotification(_tenantId, _userId1, "IncidentCreated",
            NotificationCategory.Incident, NotificationSeverity.Warning, BaseTime.AddHours(-1));
        // Out-of-range: too old
        var tooOld = MakeNotification(_tenantId, _userId1, "IncidentCreated",
            NotificationCategory.Incident, NotificationSeverity.Warning, BaseTime.AddDays(-8));

        context.Notifications.AddRange(inRange, tooOld);
        await context.SaveChangesAsync();

        var svc = CreateService(context);
        var result = await svc.GetPlatformMetricsAsync(
            _tenantId,
            BaseTime.AddDays(-7),
            BaseTime);

        result.TotalGenerated.Should().Be(1);
    }

    [Fact]
    public async Task GetPlatformMetrics_FiltersByTenantId()
    {
        await using var context = CreateContext();
        var otherTenant = Guid.NewGuid();
        var mine = MakeNotification(_tenantId, _userId1, "IncidentCreated",
            NotificationCategory.Incident, NotificationSeverity.Warning, BaseTime.AddHours(-1));
        var theirs = MakeNotification(otherTenant, _userId2, "IncidentCreated",
            NotificationCategory.Incident, NotificationSeverity.Warning, BaseTime.AddHours(-1));

        context.Notifications.AddRange(mine, theirs);
        await context.SaveChangesAsync();

        var svc = CreateService(context);
        var result = await svc.GetPlatformMetricsAsync(
            _tenantId,
            BaseTime.AddDays(-1),
            BaseTime);

        result.TotalGenerated.Should().Be(1);
    }

    [Fact]
    public async Task GetPlatformMetrics_WithDeliveries_ReturnsDeliveryBreakdown()
    {
        await using var context = CreateContext();
        var n1 = MakeNotification(_tenantId, _userId1, "IncidentCreated",
            NotificationCategory.Incident, NotificationSeverity.Critical, DateTimeOffset.UtcNow.AddHours(-1));
        context.Notifications.Add(n1);

        var d1 = NotificationDelivery.Create(n1.Id, DeliveryChannel.Email, "user@test.com");
        d1.IncrementRetry();
        d1.MarkDelivered();

        var d2 = NotificationDelivery.Create(n1.Id, DeliveryChannel.InApp);
        d2.IncrementRetry();
        d2.MarkFailed("channel error");

        context.Deliveries.AddRange(d1, d2);
        await context.SaveChangesAsync();

        // Use UtcNow-relative range so delivery.CreatedAt (= UtcNow) falls within it
        var now = DateTimeOffset.UtcNow;
        var svc = CreateService(context);
        var result = await svc.GetPlatformMetricsAsync(
            _tenantId,
            now.AddDays(-1),
            now.AddMinutes(1));

        result.DeliveriesByChannel.Should().ContainKey("Email").WhoseValue.Should().Be(1);
        result.DeliveriesByChannel.Should().ContainKey("InApp").WhoseValue.Should().Be(1);
        result.TotalDelivered.Should().Be(1);
        result.TotalFailed.Should().Be(1);
    }

    // ── GetInteractionMetrics ────────────────────────────────────────────────

    [Fact]
    public async Task GetInteractionMetrics_EmptyDb_ReturnsZeroRates()
    {
        await using var context = CreateContext();
        var svc = CreateService(context);

        var result = await svc.GetInteractionMetricsAsync(
            _tenantId,
            BaseTime.AddDays(-7),
            BaseTime);

        result.TotalRead.Should().Be(0);
        result.TotalUnread.Should().Be(0);
        result.ReadRate.Should().Be(0m);
        result.AcknowledgeRate.Should().Be(0m);
        result.AverageTimeToReadMinutes.Should().Be(0m);
    }

    [Fact]
    public async Task GetInteractionMetrics_ReadRate_CalculatedCorrectly()
    {
        await using var context = CreateContext();
        // 2 read, 1 unread → readRate = 2/3
        var n1 = MakeNotification(_tenantId, _userId1, "A",
            NotificationCategory.Incident, NotificationSeverity.Info, BaseTime.AddHours(-2));
        n1.MarkAsRead();

        var n2 = MakeNotification(_tenantId, _userId1, "A",
            NotificationCategory.Incident, NotificationSeverity.Info, BaseTime.AddHours(-2));
        n2.MarkAsRead();

        var n3 = MakeNotification(_tenantId, _userId1, "A",
            NotificationCategory.Incident, NotificationSeverity.Info, BaseTime.AddHours(-2));
        // unread — no action

        context.Notifications.AddRange(n1, n2, n3);
        await context.SaveChangesAsync();

        var svc = CreateService(context);
        var result = await svc.GetInteractionMetricsAsync(
            _tenantId,
            BaseTime.AddDays(-1),
            BaseTime);

        result.TotalRead.Should().Be(2);
        result.TotalUnread.Should().Be(1);
        result.ReadRate.Should().BeApproximately(0.6667m, 0.0001m);
    }

    [Fact]
    public async Task GetInteractionMetrics_AcknowledgeRate_OnlyCountsRequiresAction()
    {
        await using var context = CreateContext();
        // 1 requires action + acknowledged, 1 requires action + not acknowledged, 2 no action
        var n1 = MakeNotification(_tenantId, _userId1, "A",
            NotificationCategory.Approval, NotificationSeverity.Warning, BaseTime.AddHours(-3),
            requiresAction: true);
        n1.Acknowledge(_userId1);

        var n2 = MakeNotification(_tenantId, _userId1, "A",
            NotificationCategory.Approval, NotificationSeverity.Warning, BaseTime.AddHours(-3),
            requiresAction: true);
        // not acknowledged

        var n3 = MakeNotification(_tenantId, _userId1, "A",
            NotificationCategory.Incident, NotificationSeverity.Info, BaseTime.AddHours(-3),
            requiresAction: false);
        var n4 = MakeNotification(_tenantId, _userId1, "A",
            NotificationCategory.Incident, NotificationSeverity.Info, BaseTime.AddHours(-3),
            requiresAction: false);

        context.Notifications.AddRange(n1, n2, n3, n4);
        await context.SaveChangesAsync();

        var svc = CreateService(context);
        var result = await svc.GetInteractionMetricsAsync(
            _tenantId,
            BaseTime.AddDays(-1),
            BaseTime);

        result.TotalAcknowledged.Should().Be(1);
        result.AcknowledgeRate.Should().BeApproximately(0.5m, 0.0001m);
        result.TotalUnacknowledgedActionRequired.Should().Be(1);
    }

    [Fact]
    public async Task GetInteractionMetrics_EscalatedAndSnoozed_CountedCorrectly()
    {
        await using var context = CreateContext();
        var n1 = MakeNotification(_tenantId, _userId1, "A",
            NotificationCategory.Incident, NotificationSeverity.Critical, BaseTime.AddHours(-2));
        n1.MarkAsEscalated();

        var n2 = MakeNotification(_tenantId, _userId1, "A",
            NotificationCategory.Incident, NotificationSeverity.Warning, BaseTime.AddHours(-2));
        n2.Snooze(BaseTime.AddHours(2), _userId1);

        context.Notifications.AddRange(n1, n2);
        await context.SaveChangesAsync();

        var svc = CreateService(context);
        var result = await svc.GetInteractionMetricsAsync(
            _tenantId,
            BaseTime.AddDays(-1),
            BaseTime);

        result.TotalEscalated.Should().Be(1);
        result.TotalSnoozed.Should().Be(1);
    }

    // ── GetQualityMetrics ────────────────────────────────────────────────────

    [Fact]
    public async Task GetQualityMetrics_EmptyDb_ReturnsZeroMetrics()
    {
        await using var context = CreateContext();
        var svc = CreateService(context);

        var result = await svc.GetQualityMetricsAsync(
            _tenantId,
            BaseTime.AddDays(-7),
            BaseTime);

        result.AveragePerUserPerDay.Should().Be(0m);
        result.TotalSuppressed.Should().Be(0);
        result.TotalGrouped.Should().Be(0);
        result.TotalCorrelatedWithIncidents.Should().Be(0);
        result.TopNoisyTypes.Should().BeEmpty();
        result.LeastEngagedTypes.Should().BeEmpty();
        result.UnacknowledgedActionTypes.Should().BeEmpty();
    }

    [Fact]
    public async Task GetQualityMetrics_WithSuppressedAndGrouped_ReturnsCounts()
    {
        await using var context = CreateContext();
        var groupId = Guid.NewGuid();
        var incidentId = Guid.NewGuid();

        var n1 = MakeNotification(_tenantId, _userId1, "A",
            NotificationCategory.Incident, NotificationSeverity.Info, BaseTime.AddHours(-1));
        n1.Suppress("duplicate");

        var n2 = MakeNotification(_tenantId, _userId1, "A",
            NotificationCategory.Incident, NotificationSeverity.Info, BaseTime.AddHours(-1));
        n2.SetCorrelation("corr-key", groupId);

        var n3 = MakeNotification(_tenantId, _userId1, "A",
            NotificationCategory.Incident, NotificationSeverity.Info, BaseTime.AddHours(-1));
        n3.CorrelateWithIncident(incidentId);

        context.Notifications.AddRange(n1, n2, n3);
        await context.SaveChangesAsync();

        var svc = CreateService(context);
        var result = await svc.GetQualityMetricsAsync(
            _tenantId,
            BaseTime.AddDays(-1),
            BaseTime);

        result.TotalSuppressed.Should().Be(1);
        result.TotalGrouped.Should().Be(1);
        result.TotalCorrelatedWithIncidents.Should().Be(1);
    }

    [Fact]
    public async Task GetQualityMetrics_TopNoisyTypes_ReturnsSortedDescByCount()
    {
        await using var context = CreateContext();
        // Type A: 5, Type B: 3, Type C: 1
        for (var i = 0; i < 5; i++)
            context.Notifications.Add(MakeNotification(_tenantId, _userId1, "TypeA",
                NotificationCategory.Change, NotificationSeverity.Info, BaseTime.AddHours(-1)));
        for (var i = 0; i < 3; i++)
            context.Notifications.Add(MakeNotification(_tenantId, _userId1, "TypeB",
                NotificationCategory.Change, NotificationSeverity.Info, BaseTime.AddHours(-1)));
        context.Notifications.Add(MakeNotification(_tenantId, _userId1, "TypeC",
            NotificationCategory.Change, NotificationSeverity.Info, BaseTime.AddHours(-1)));

        await context.SaveChangesAsync();

        var svc = CreateService(context);
        var result = await svc.GetQualityMetricsAsync(
            _tenantId,
            BaseTime.AddDays(-1),
            BaseTime);

        result.TopNoisyTypes.Should().HaveCountGreaterThanOrEqualTo(2);
        result.TopNoisyTypes[0].EventType.Should().Be("TypeA");
        result.TopNoisyTypes[0].Count.Should().Be(5);
        result.TopNoisyTypes[1].EventType.Should().Be("TypeB");
    }

    [Fact]
    public async Task GetQualityMetrics_LeastEngaged_RequiresMinimumOf3Samples()
    {
        await using var context = CreateContext();
        // Type A: 5 notifications, all unread → lowest engagement — qualifies (≥3)
        for (var i = 0; i < 5; i++)
            context.Notifications.Add(MakeNotification(_tenantId, _userId1, "TypeA",
                NotificationCategory.Incident, NotificationSeverity.Info, BaseTime.AddHours(-1)));
        // Type B: 2 notifications — does NOT qualify (< 3)
        for (var i = 0; i < 2; i++)
            context.Notifications.Add(MakeNotification(_tenantId, _userId1, "TypeB",
                NotificationCategory.Incident, NotificationSeverity.Info, BaseTime.AddHours(-1)));

        await context.SaveChangesAsync();

        var svc = CreateService(context);
        var result = await svc.GetQualityMetricsAsync(
            _tenantId,
            BaseTime.AddDays(-1),
            BaseTime);

        // TypeB has < 3 samples, should not appear in leastEngaged
        result.LeastEngagedTypes.Select(t => t.EventType).Should().NotContain("TypeB");
        result.LeastEngagedTypes.Select(t => t.EventType).Should().Contain("TypeA");
    }

    [Fact]
    public async Task GetQualityMetrics_FiltersByTenantId()
    {
        await using var context = CreateContext();
        var otherTenant = Guid.NewGuid();

        context.Notifications.Add(MakeNotification(_tenantId, _userId1, "A",
            NotificationCategory.Incident, NotificationSeverity.Info, BaseTime.AddHours(-1)));
        context.Notifications.Add(MakeNotification(otherTenant, _userId2, "A",
            NotificationCategory.Incident, NotificationSeverity.Info, BaseTime.AddHours(-1)));

        await context.SaveChangesAsync();

        var svc = CreateService(context);
        var result = await svc.GetQualityMetricsAsync(
            _tenantId,
            BaseTime.AddDays(-1),
            BaseTime);

        // Only 1 user in tenant, 1 notification, period ≈ 1 day → avg ≈ 1
        result.AveragePerUserPerDay.Should().BeGreaterThan(0m);
        result.TopNoisyTypes.Should().HaveCount(1);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private NotificationsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseInMemoryDatabase($"ntf-metrics-{Guid.NewGuid():N}")
            .Options;

        return new NotificationsDbContext(
            options,
            new TestCurrentTenant(_tenantId),
            new TestCurrentUser(),
            new TestDateTimeProvider(BaseTime));
    }

    private static NotificationMetricsService CreateService(NotificationsDbContext context) =>
        new(context, NullLogger<NotificationMetricsService>.Instance);

    /// <summary>
    /// Cria uma notificação de teste e ajusta CreatedAt via reflexão
    /// (a propriedade é private set — definida no construtor privado).
    /// </summary>
    private static Notification MakeNotification(
        Guid tenantId,
        Guid recipientUserId,
        string eventType,
        NotificationCategory category,
        NotificationSeverity severity,
        DateTimeOffset createdAt,
        bool requiresAction = false)
    {
        var n = Notification.Create(
            tenantId,
            recipientUserId,
            eventType,
            category,
            severity,
            $"Title {eventType}",
            $"Message for {eventType}",
            "TestModule",
            requiresAction: requiresAction);

        // CreatedAt é private set — usa reflexão para controlar o timestamp nos testes
        var prop = typeof(Notification).GetProperty(
            nameof(Notification.CreatedAt),
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)!;
        prop.SetValue(n, createdAt);

        return n;
    }

    private sealed class TestCurrentTenant(Guid id) : ICurrentTenant
    {
        public Guid Id => id;
        public string Slug => "metrics-test";
        public string Name => "Metrics Test Tenant";
        public bool IsActive => true;
        public bool HasCapability(string capability) => true;
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public string Id => "metrics-test-user";
        public string Name => "Metrics Test";
        public string Email => "metrics.test@nextraceone.local";
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
