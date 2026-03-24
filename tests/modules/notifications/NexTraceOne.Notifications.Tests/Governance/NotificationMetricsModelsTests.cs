using NexTraceOne.Notifications.Application.Abstractions;

namespace NexTraceOne.Notifications.Tests.Governance;

/// <summary>
/// Testes para os DTOs de métricas da plataforma de notificações — Fase 7.
/// Valida contratos, defaults e consistência dos records de métricas.
/// </summary>
public sealed class NotificationMetricsModelsTests
{
    [Fact]
    public void NotificationPlatformMetrics_Defaults_ShouldBeConsistent()
    {
        var metrics = new NotificationPlatformMetrics();

        metrics.TotalGenerated.Should().Be(0);
        metrics.ByCategory.Should().NotBeNull().And.BeEmpty();
        metrics.BySeverity.Should().NotBeNull().And.BeEmpty();
        metrics.BySourceModule.Should().NotBeNull().And.BeEmpty();
        metrics.DeliveriesByChannel.Should().NotBeNull().And.BeEmpty();
        metrics.TotalDelivered.Should().Be(0);
        metrics.TotalFailed.Should().Be(0);
        metrics.TotalPending.Should().Be(0);
        metrics.TotalSkipped.Should().Be(0);
    }

    [Fact]
    public void NotificationPlatformMetrics_WithData_ShouldHoldValues()
    {
        var metrics = new NotificationPlatformMetrics
        {
            TotalGenerated = 100,
            ByCategory = new Dictionary<string, int> { ["Incident"] = 50, ["Approval"] = 30 },
            BySeverity = new Dictionary<string, int> { ["Critical"] = 20, ["Info"] = 60 },
            BySourceModule = new Dictionary<string, int> { ["OperationalIntelligence"] = 50 },
            DeliveriesByChannel = new Dictionary<string, int> { ["Email"] = 80, ["MicrosoftTeams"] = 40 },
            TotalDelivered = 110,
            TotalFailed = 10,
            TotalPending = 5,
            TotalSkipped = 3
        };

        metrics.TotalGenerated.Should().Be(100);
        metrics.ByCategory.Should().HaveCount(2);
        metrics.TotalDelivered.Should().Be(110);
        metrics.TotalFailed.Should().Be(10);
    }

    [Fact]
    public void NotificationInteractionMetrics_Defaults_ShouldBeConsistent()
    {
        var metrics = new NotificationInteractionMetrics();

        metrics.TotalRead.Should().Be(0);
        metrics.TotalUnread.Should().Be(0);
        metrics.TotalAcknowledged.Should().Be(0);
        metrics.TotalSnoozed.Should().Be(0);
        metrics.TotalArchived.Should().Be(0);
        metrics.TotalDismissed.Should().Be(0);
        metrics.TotalEscalated.Should().Be(0);
        metrics.ReadRate.Should().Be(0);
        metrics.AcknowledgeRate.Should().Be(0);
    }

    [Fact]
    public void NotificationInteractionMetrics_WithData_ShouldComputeCorrectly()
    {
        var metrics = new NotificationInteractionMetrics
        {
            TotalRead = 80,
            TotalUnread = 20,
            TotalAcknowledged = 15,
            TotalSnoozed = 5,
            TotalArchived = 10,
            TotalDismissed = 3,
            TotalEscalated = 2,
            ReadRate = 0.80m,
            AcknowledgeRate = 0.75m
        };

        metrics.TotalRead.Should().Be(80);
        metrics.TotalUnread.Should().Be(20);
        metrics.ReadRate.Should().Be(0.80m);
        metrics.AcknowledgeRate.Should().Be(0.75m);
    }

    [Fact]
    public void NotificationQualityMetrics_Defaults_ShouldBeConsistent()
    {
        var metrics = new NotificationQualityMetrics();

        metrics.AveragePerUserPerDay.Should().Be(0);
        metrics.TotalSuppressed.Should().Be(0);
        metrics.TotalGrouped.Should().Be(0);
        metrics.TotalCorrelatedWithIncidents.Should().Be(0);
        metrics.TopNoisyTypes.Should().NotBeNull().And.BeEmpty();
        metrics.LeastEngagedTypes.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void NotificationQualityMetrics_WithData_ShouldHoldValues()
    {
        var metrics = new NotificationQualityMetrics
        {
            AveragePerUserPerDay = 12.5m,
            TotalSuppressed = 50,
            TotalGrouped = 30,
            TotalCorrelatedWithIncidents = 10,
            TopNoisyTypes = [new("IncidentCreated", 200), new("BudgetExceeded", 100)],
            LeastEngagedTypes = [new("EvidenceExpiring", 50)]
        };

        metrics.AveragePerUserPerDay.Should().Be(12.5m);
        metrics.TotalSuppressed.Should().Be(50);
        metrics.TopNoisyTypes.Should().HaveCount(2);
        metrics.LeastEngagedTypes.Should().HaveCount(1);
    }

    [Fact]
    public void NotificationTypeCount_ShouldHoldValues()
    {
        var count = new NotificationTypeCount("IncidentCreated", 42);

        count.EventType.Should().Be("IncidentCreated");
        count.Count.Should().Be(42);
    }

    [Fact]
    public void NotificationHealthReport_ShouldHoldValues()
    {
        var report = new NotificationHealthReport
        {
            OverallStatus = NotificationHealthStatus.Healthy,
            Components =
            [
                new NotificationComponentHealth
                {
                    Name = "InAppStore",
                    Status = NotificationHealthStatus.Healthy,
                    Description = "OK"
                }
            ]
        };

        report.OverallStatus.Should().Be(NotificationHealthStatus.Healthy);
        report.Components.Should().HaveCount(1);
        report.CheckedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void NotificationHealthReport_WithDegraded_ShouldReflect()
    {
        var report = new NotificationHealthReport
        {
            OverallStatus = NotificationHealthStatus.Degraded,
            Components =
            [
                new NotificationComponentHealth
                {
                    Name = "EmailChannel",
                    Status = NotificationHealthStatus.Degraded,
                    Description = "5 failures in last hour",
                    Metadata = new Dictionary<string, string>
                    {
                        ["RecentFailures"] = "5",
                        ["WindowMinutes"] = "60"
                    }
                }
            ]
        };

        report.OverallStatus.Should().Be(NotificationHealthStatus.Degraded);
        report.Components[0].Metadata.Should().ContainKey("RecentFailures");
    }

    [Fact]
    public void CatalogGovernanceSummary_ShouldHoldValues()
    {
        var summary = new CatalogGovernanceSummary
        {
            TotalEventTypes = 29,
            TypesWithTemplate = 11,
            TypesWithoutTemplate = ["IncidentResolved", "AnomalyDetected"],
            MandatoryTypes = 5,
            ChannelStatus = new Dictionary<string, bool>
            {
                ["InApp"] = true,
                ["Email"] = true,
                ["MicrosoftTeams"] = true
            },
            TotalCategories = 11
        };

        summary.TotalEventTypes.Should().Be(29);
        summary.TypesWithTemplate.Should().Be(11);
        summary.TypesWithoutTemplate.Should().HaveCount(2);
        summary.MandatoryTypes.Should().Be(5);
        summary.ChannelStatus.Should().HaveCount(3);
    }

    [Fact]
    public void CatalogValidationResult_Valid_ShouldReflect()
    {
        var result = new CatalogValidationResult
        {
            IsValid = true,
            EventType = "IncidentCreated",
            HasTemplate = true,
            IsMandatory = true,
            Messages = []
        };

        result.IsValid.Should().BeTrue();
        result.HasTemplate.Should().BeTrue();
        result.IsMandatory.Should().BeTrue();
        result.Messages.Should().BeEmpty();
    }

    [Fact]
    public void CatalogValidationResult_Invalid_ShouldHaveMessages()
    {
        var result = new CatalogValidationResult
        {
            IsValid = false,
            EventType = "UnknownType",
            HasTemplate = false,
            IsMandatory = false,
            Messages = ["Event type 'UnknownType' is not registered in the notification catalog."]
        };

        result.IsValid.Should().BeFalse();
        result.Messages.Should().HaveCount(1);
    }

    [Fact]
    public void NotificationAuditEntry_ShouldHoldValues()
    {
        var entry = new NotificationAuditEntry
        {
            TenantId = Guid.NewGuid(),
            ActionType = NotificationAuditActions.CriticalNotificationGenerated,
            ResourceId = "notif-123",
            ResourceType = "Notification",
            PerformedBy = Guid.NewGuid(),
            Description = "Critical notification generated"
        };

        entry.ActionType.Should().Be("notification.critical.generated");
        entry.ResourceType.Should().Be("Notification");
        entry.OccurredAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void NotificationAuditActions_ShouldHaveAllExpectedActions()
    {
        NotificationAuditActions.CriticalNotificationGenerated.Should().NotBeNullOrWhiteSpace();
        NotificationAuditActions.CriticalNotificationDelivered.Should().NotBeNullOrWhiteSpace();
        NotificationAuditActions.CriticalNotificationFailed.Should().NotBeNullOrWhiteSpace();
        NotificationAuditActions.NotificationAcknowledged.Should().NotBeNullOrWhiteSpace();
        NotificationAuditActions.NotificationSnoozed.Should().NotBeNullOrWhiteSpace();
        NotificationAuditActions.NotificationEscalated.Should().NotBeNullOrWhiteSpace();
        NotificationAuditActions.IncidentCreatedFromNotification.Should().NotBeNullOrWhiteSpace();
        NotificationAuditActions.PreferencesChanged.Should().NotBeNullOrWhiteSpace();
        NotificationAuditActions.NotificationSuppressed.Should().NotBeNullOrWhiteSpace();
    }
}
