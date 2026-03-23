using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Infrastructure.Preferences;

namespace NexTraceOne.Notifications.Tests.Preferences;

public sealed class MandatoryNotificationPolicyTests
{
    private readonly MandatoryNotificationPolicy _policy = new();

    // ── IsMandatory tests ──

    [Fact]
    public void IsMandatory_BreakGlassActivated_ReturnsTrue()
    {
        var result = _policy.IsMandatory(
            NotificationType.BreakGlassActivated, NotificationCategory.Security, NotificationSeverity.Critical);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsMandatory_CriticalIncident_ReturnsTrue()
    {
        var result = _policy.IsMandatory(
            NotificationType.IncidentCreated, NotificationCategory.Incident, NotificationSeverity.Critical);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsMandatory_ApprovalPending_ReturnsTrue()
    {
        var result = _policy.IsMandatory(
            NotificationType.ApprovalPending, NotificationCategory.Approval, NotificationSeverity.ActionRequired);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsMandatory_ComplianceCheckFailed_ReturnsTrue()
    {
        var result = _policy.IsMandatory(
            NotificationType.ComplianceCheckFailed, NotificationCategory.Compliance, NotificationSeverity.Warning);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsMandatory_CriticalSeverity_ReturnsTrue()
    {
        var result = _policy.IsMandatory(
            NotificationType.BudgetExceeded, NotificationCategory.FinOps, NotificationSeverity.Critical);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsMandatory_InfoSeverity_ReturnsFalse()
    {
        var result = _policy.IsMandatory(
            NotificationType.BudgetExceeded, NotificationCategory.FinOps, NotificationSeverity.Info);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsMandatory_WarningNotSpecialEvent_ReturnsFalse()
    {
        var result = _policy.IsMandatory(
            NotificationType.IntegrationFailed, NotificationCategory.Integration, NotificationSeverity.Warning);

        result.Should().BeFalse();
    }

    // ── GetMandatoryChannels tests ──

    [Fact]
    public void GetMandatoryChannels_BreakGlass_AllChannels()
    {
        var channels = _policy.GetMandatoryChannels(
            NotificationType.BreakGlassActivated, NotificationCategory.Security, NotificationSeverity.Critical);

        channels.Should().BeEquivalentTo(
            [DeliveryChannel.InApp, DeliveryChannel.Email, DeliveryChannel.MicrosoftTeams]);
    }

    [Fact]
    public void GetMandatoryChannels_CriticalIncident_AllChannels()
    {
        var channels = _policy.GetMandatoryChannels(
            NotificationType.IncidentCreated, NotificationCategory.Incident, NotificationSeverity.Critical);

        channels.Should().BeEquivalentTo(
            [DeliveryChannel.InApp, DeliveryChannel.Email, DeliveryChannel.MicrosoftTeams]);
    }

    [Fact]
    public void GetMandatoryChannels_ApprovalPending_InAppAndEmail()
    {
        var channels = _policy.GetMandatoryChannels(
            NotificationType.ApprovalPending, NotificationCategory.Approval, NotificationSeverity.ActionRequired);

        channels.Should().BeEquivalentTo(
            [DeliveryChannel.InApp, DeliveryChannel.Email]);
    }

    [Fact]
    public void GetMandatoryChannels_ComplianceCheckFailed_InAppAndEmail()
    {
        var channels = _policy.GetMandatoryChannels(
            NotificationType.ComplianceCheckFailed, NotificationCategory.Compliance, NotificationSeverity.Warning);

        channels.Should().BeEquivalentTo(
            [DeliveryChannel.InApp, DeliveryChannel.Email]);
    }

    [Fact]
    public void GetMandatoryChannels_CriticalSeverity_InAppAndEmail()
    {
        var channels = _policy.GetMandatoryChannels(
            NotificationType.BudgetExceeded, NotificationCategory.FinOps, NotificationSeverity.Critical);

        channels.Should().BeEquivalentTo(
            [DeliveryChannel.InApp, DeliveryChannel.Email]);
    }

    [Fact]
    public void GetMandatoryChannels_InfoSeverity_Empty()
    {
        var channels = _policy.GetMandatoryChannels(
            NotificationType.BudgetExceeded, NotificationCategory.FinOps, NotificationSeverity.Info);

        channels.Should().BeEmpty();
    }
}
