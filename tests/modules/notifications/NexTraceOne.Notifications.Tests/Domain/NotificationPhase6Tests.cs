using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Tests.Domain;

/// <summary>
/// Testes de unidade para as funcionalidades da Fase 6 na entidade Notification:
/// acknowledge enriquecido, snooze, correlação, agrupamento, escalação,
/// correlação com incidente e supressão.
/// </summary>
public sealed class NotificationPhase6Tests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _recipientId = Guid.NewGuid();

    // ── Acknowledge enriquecido ──

    [Fact]
    public void Acknowledge_WithUserIdAndComment_ShouldSetAllFields()
    {
        var notification = CreateTestNotification();
        var userId = Guid.NewGuid();

        notification.Acknowledge(userId, "Investigating the root cause");

        notification.Status.Should().Be(NotificationStatus.Acknowledged);
        notification.AcknowledgedAt.Should().NotBeNull();
        notification.AcknowledgedBy.Should().Be(userId);
        notification.AcknowledgeComment.Should().Be("Investigating the root cause");
        notification.ReadAt.Should().NotBeNull(); // implicitly read
    }

    [Fact]
    public void Acknowledge_WithoutParameters_ShouldStillWork()
    {
        var notification = CreateTestNotification();

        notification.Acknowledge();

        notification.Status.Should().Be(NotificationStatus.Acknowledged);
        notification.AcknowledgedBy.Should().BeNull();
        notification.AcknowledgeComment.Should().BeNull();
    }

    [Fact]
    public void Acknowledge_FromArchived_ShouldNotChange()
    {
        var notification = CreateTestNotification();
        notification.Archive();

        notification.Acknowledge(Guid.NewGuid(), "late ack");

        notification.Status.Should().Be(NotificationStatus.Archived);
        notification.AcknowledgedBy.Should().BeNull();
    }

    // ── Snooze ──

    [Fact]
    public void Snooze_ShouldSetSnoozedFields()
    {
        var notification = CreateTestNotification();
        var userId = Guid.NewGuid();
        var until = DateTimeOffset.UtcNow.AddHours(4);

        notification.Snooze(until, userId);

        notification.SnoozedUntil.Should().Be(until);
        notification.SnoozedBy.Should().Be(userId);
    }

    [Fact]
    public void IsSnoozed_WhenSnoozedUntilFuture_ShouldReturnTrue()
    {
        var notification = CreateTestNotification();
        notification.Snooze(DateTimeOffset.UtcNow.AddHours(1), Guid.NewGuid());

        notification.IsSnoozed().Should().BeTrue();
    }

    [Fact]
    public void IsSnoozed_WhenSnoozedUntilPast_ShouldReturnFalse()
    {
        var notification = CreateTestNotification();
        notification.Snooze(DateTimeOffset.UtcNow.AddHours(-1), Guid.NewGuid());

        notification.IsSnoozed().Should().BeFalse();
    }

    [Fact]
    public void IsSnoozed_WhenNotSnoozed_ShouldReturnFalse()
    {
        var notification = CreateTestNotification();

        notification.IsSnoozed().Should().BeFalse();
    }

    [Fact]
    public void Unsnooze_ShouldClearSnoozedFields()
    {
        var notification = CreateTestNotification();
        notification.Snooze(DateTimeOffset.UtcNow.AddHours(4), Guid.NewGuid());

        notification.Unsnooze();

        notification.SnoozedUntil.Should().BeNull();
        notification.SnoozedBy.Should().BeNull();
        notification.IsSnoozed().Should().BeFalse();
    }

    [Fact]
    public void Snooze_WhenArchived_ShouldNotChange()
    {
        var notification = CreateTestNotification();
        notification.Archive();

        notification.Snooze(DateTimeOffset.UtcNow.AddHours(1), Guid.NewGuid());

        notification.SnoozedUntil.Should().BeNull();
    }

    [Fact]
    public void Snooze_WhenDismissed_ShouldNotChange()
    {
        var notification = CreateTestNotification();
        notification.Dismiss();

        notification.Snooze(DateTimeOffset.UtcNow.AddHours(1), Guid.NewGuid());

        notification.SnoozedUntil.Should().BeNull();
    }

    // ── Correlation & Grouping ──

    [Fact]
    public void SetCorrelation_ShouldSetCorrelationKeyAndGroupId()
    {
        var notification = CreateTestNotification();
        var groupId = Guid.NewGuid();

        notification.SetCorrelation("tenant|module|IncidentCreated|Incident|123", groupId);

        notification.CorrelationKey.Should().Be("tenant|module|IncidentCreated|Incident|123");
        notification.GroupId.Should().Be(groupId);
    }

    [Fact]
    public void SetCorrelation_WithNullGroupId_ShouldSetOnlyKey()
    {
        var notification = CreateTestNotification();

        notification.SetCorrelation("key-only");

        notification.CorrelationKey.Should().Be("key-only");
        notification.GroupId.Should().BeNull();
    }

    // ── Occurrence Count ──

    [Fact]
    public void IncrementOccurrence_ShouldIncrementCountAndUpdateTimestamp()
    {
        var notification = CreateTestNotification();
        notification.OccurrenceCount.Should().Be(1);
        notification.LastOccurrenceAt.Should().BeNull();

        notification.IncrementOccurrence();

        notification.OccurrenceCount.Should().Be(2);
        notification.LastOccurrenceAt.Should().NotBeNull();
        notification.LastOccurrenceAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void IncrementOccurrence_Multiple_ShouldAccumulate()
    {
        var notification = CreateTestNotification();

        notification.IncrementOccurrence();
        notification.IncrementOccurrence();
        notification.IncrementOccurrence();

        notification.OccurrenceCount.Should().Be(4);
    }

    // ── Escalation ──

    [Fact]
    public void MarkAsEscalated_ShouldSetEscalationFields()
    {
        var notification = CreateTestNotification();

        notification.MarkAsEscalated();

        notification.IsEscalated.Should().BeTrue();
        notification.EscalatedAt.Should().NotBeNull();
        notification.EscalatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkAsEscalated_Idempotent_ShouldNotUpdateTimestamp()
    {
        var notification = CreateTestNotification();
        notification.MarkAsEscalated();
        var firstEscalatedAt = notification.EscalatedAt;

        notification.MarkAsEscalated(); // idempotent

        notification.EscalatedAt.Should().Be(firstEscalatedAt);
    }

    [Fact]
    public void NewNotification_ShouldNotBeEscalated()
    {
        var notification = CreateTestNotification();

        notification.IsEscalated.Should().BeFalse();
        notification.EscalatedAt.Should().BeNull();
    }

    // ── Incident Correlation ──

    [Fact]
    public void CorrelateWithIncident_ShouldSetIncidentId()
    {
        var notification = CreateTestNotification();
        var incidentId = Guid.NewGuid();

        notification.CorrelateWithIncident(incidentId);

        notification.CorrelatedIncidentId.Should().Be(incidentId);
    }

    [Fact]
    public void NewNotification_ShouldHaveNoCorrelatedIncident()
    {
        var notification = CreateTestNotification();

        notification.CorrelatedIncidentId.Should().BeNull();
    }

    // ── Suppression ──

    [Fact]
    public void Suppress_ShouldSetSuppressionFields()
    {
        var notification = CreateTestNotification();

        notification.Suppress("Already acknowledged for same entity");

        notification.IsSuppressed.Should().BeTrue();
        notification.SuppressionReason.Should().Be("Already acknowledged for same entity");
    }

    [Fact]
    public void Suppress_EmptyReason_ShouldThrow()
    {
        var notification = CreateTestNotification();

        var act = () => notification.Suppress("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NewNotification_ShouldNotBeSuppressed()
    {
        var notification = CreateTestNotification();

        notification.IsSuppressed.Should().BeFalse();
        notification.SuppressionReason.Should().BeNull();
    }

    // ── Default state of new Phase 6 fields ──

    [Fact]
    public void NewNotification_ShouldHavePhase6Defaults()
    {
        var notification = CreateTestNotification();

        notification.CorrelationKey.Should().BeNull();
        notification.GroupId.Should().BeNull();
        notification.OccurrenceCount.Should().Be(1);
        notification.LastOccurrenceAt.Should().BeNull();
        notification.SnoozedUntil.Should().BeNull();
        notification.SnoozedBy.Should().BeNull();
        notification.AcknowledgedBy.Should().BeNull();
        notification.AcknowledgeComment.Should().BeNull();
        notification.IsEscalated.Should().BeFalse();
        notification.EscalatedAt.Should().BeNull();
        notification.CorrelatedIncidentId.Should().BeNull();
        notification.IsSuppressed.Should().BeFalse();
        notification.SuppressionReason.Should().BeNull();
    }

    private Notification CreateTestNotification() =>
        Notification.Create(
            _tenantId, _recipientId,
            "TestEvent", NotificationCategory.Informational,
            NotificationSeverity.Info,
            "Test notification", "Test message body.",
            "TestModule");
}
