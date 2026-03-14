using FluentAssertions;
using NexTraceOne.BuildingBlocks.Core.Notifications;
using Xunit;

namespace NexTraceOne.BuildingBlocks.Core.Tests.Notifications;

/// <summary>
/// Testes unitários para os modelos de notificação do BuildingBlocks.Domain.
/// Cobre criação, transições de estado, enums e identificadores fortemente tipados.
/// </summary>
public sealed class NotificationModelsTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2025, 06, 01, 12, 0, 0, TimeSpan.Zero);

    // ─── NotificationRequest ─────────────────────────────────────────

    [Fact]
    public void NotificationRequest_Create_ShouldSetProperties()
    {
        var request = NotificationRequest.Create(
            TenantId,
            NotificationCategory.Workflow,
            NotificationSeverity.Alert,
            "workflow.approval_requested",
            "notification.workflow.approval.subject",
            "notification.workflow.approval.body",
            "ChangeGovernance",
            parameters: "{\"release\":\"v2.0\"}",
            deepLinkUrl: "/releases/123",
            requiresAcknowledgement: true,
            expiresAt: Now.AddDays(7),
            correlationId: "corr-001",
            createdByUserId: UserId);

        request.Id.Value.Should().NotBeEmpty();
        request.TenantId.Should().Be(TenantId);
        request.Category.Should().Be(NotificationCategory.Workflow);
        request.Severity.Should().Be(NotificationSeverity.Alert);
        request.TemplateCode.Should().Be("workflow.approval_requested");
        request.SubjectKey.Should().Be("notification.workflow.approval.subject");
        request.BodyKey.Should().Be("notification.workflow.approval.body");
        request.SourceModule.Should().Be("ChangeGovernance");
        request.Parameters.Should().Be("{\"release\":\"v2.0\"}");
        request.DeepLinkUrl.Should().Be("/releases/123");
        request.RequiresAcknowledgement.Should().BeTrue();
        request.ExpiresAt.Should().Be(Now.AddDays(7));
        request.CorrelationId.Should().Be("corr-001");
        request.CreatedByUserId.Should().Be(UserId);
    }

    [Fact]
    public void NotificationRequest_Create_WithMinimalParams_ShouldWork()
    {
        var request = NotificationRequest.Create(
            TenantId,
            NotificationCategory.System,
            NotificationSeverity.Info,
            "system.maintenance",
            "notification.system.subject",
            "notification.system.body",
            "Platform");

        request.Id.Value.Should().NotBeEmpty();
        request.Parameters.Should().Be("{}");
        request.DeepLinkUrl.Should().BeNull();
        request.RequiresAcknowledgement.Should().BeFalse();
        request.ExpiresAt.Should().BeNull();
        request.CorrelationId.Should().BeNull();
        request.CreatedByUserId.Should().BeNull();
    }

    // ─── NotificationTemplate ────────────────────────────────────────

    [Fact]
    public void NotificationTemplate_Create_ShouldSetProperties()
    {
        var template = NotificationTemplate.Create(
            "workflow.approval_requested",
            NotificationCategory.Workflow,
            "Aprovação pendente: {releaseName}",
            "O release {releaseName} aguarda sua aprovação.",
            NotificationChannel.Email,
            NotificationSeverity.Alert,
            "InApp,Email,MicrosoftTeams",
            supportsI18n: true);

        template.Id.Value.Should().NotBeEmpty();
        template.Code.Should().Be("workflow.approval_requested");
        template.Category.Should().Be(NotificationCategory.Workflow);
        template.SubjectTemplate.Should().Contain("{releaseName}");
        template.BodyTemplate.Should().Contain("{releaseName}");
        template.DefaultChannel.Should().Be(NotificationChannel.Email);
        template.DefaultSeverity.Should().Be(NotificationSeverity.Alert);
        template.IsActive.Should().BeTrue();
        template.SupportedChannels.Should().Contain("Email");
        template.SupportsI18n.Should().BeTrue();
    }

    [Fact]
    public void NotificationTemplate_Deactivate_ShouldSetIsActiveFalse()
    {
        var template = NotificationTemplate.Create(
            "test.template",
            NotificationCategory.System,
            "Subject",
            "Body",
            NotificationChannel.InApp,
            NotificationSeverity.Info,
            "InApp");

        template.Deactivate();

        template.IsActive.Should().BeFalse();
    }

    // ─── NotificationPreference ──────────────────────────────────────

    [Fact]
    public void NotificationPreference_Create_ShouldSetProperties()
    {
        var pref = NotificationPreference.Create(
            UserId,
            TenantId,
            NotificationCategory.Security,
            NotificationChannel.Email,
            isEnabled: true,
            minimumSeverity: NotificationSeverity.Warning);

        pref.Id.Value.Should().NotBeEmpty();
        pref.UserId.Should().Be(UserId);
        pref.TenantId.Should().Be(TenantId);
        pref.Category.Should().Be(NotificationCategory.Security);
        pref.Channel.Should().Be(NotificationChannel.Email);
        pref.IsEnabled.Should().BeTrue();
        pref.MinimumSeverity.Should().Be(NotificationSeverity.Warning);
    }

    // ─── NotificationDelivery ────────────────────────────────────────

    [Fact]
    public void NotificationDelivery_Create_ShouldSetProperties()
    {
        var notifId = new NotificationId(Guid.NewGuid());
        var delivery = NotificationDelivery.Create(notifId, UserId, NotificationChannel.Email);

        delivery.Id.Value.Should().NotBeEmpty();
        delivery.NotificationRequestId.Should().Be(notifId);
        delivery.RecipientUserId.Should().Be(UserId);
        delivery.Channel.Should().Be(NotificationChannel.Email);
        delivery.Status.Should().Be(NotificationStatus.Pending);
        delivery.AttemptCount.Should().Be(0);
    }

    [Fact]
    public void NotificationDelivery_MarkDelivered_ShouldUpdateStatus()
    {
        var notifId = new NotificationId(Guid.NewGuid());
        var delivery = NotificationDelivery.Create(notifId, UserId, NotificationChannel.Email);

        delivery.MarkDelivered(Now);

        delivery.Status.Should().Be(NotificationStatus.Delivered);
        delivery.DeliveredAt.Should().Be(Now);
    }

    // ─── NotificationAcknowledgement ─────────────────────────────────

    [Fact]
    public void NotificationAcknowledgement_Create_ShouldSetProperties()
    {
        var notifId = new NotificationId(Guid.NewGuid());
        var ack = NotificationAcknowledgement.Create(notifId, UserId, Now, "Confirmado");

        ack.Id.Value.Should().NotBeEmpty();
        ack.NotificationRequestId.Should().Be(notifId);
        ack.UserId.Should().Be(UserId);
        ack.AcknowledgedAt.Should().Be(Now);
        ack.Comment.Should().Be("Confirmado");
    }

    // ─── NotificationRecipient ───────────────────────────────────────

    [Fact]
    public void NotificationRecipient_Create_ShouldSetProperties()
    {
        var notifId = new NotificationId(Guid.NewGuid());
        var recipient = NotificationRecipient.Create(
            notifId,
            UserId,
            TenantId,
            NotificationChannel.MicrosoftTeams);

        recipient.Id.Value.Should().NotBeEmpty();
        recipient.NotificationRequestId.Should().Be(notifId);
        recipient.UserId.Should().Be(UserId);
        recipient.TenantId.Should().Be(TenantId);
        recipient.PreferredChannel.Should().Be(NotificationChannel.MicrosoftTeams);
        recipient.Status.Should().Be(NotificationStatus.Pending);
    }

    // ─── Enums ───────────────────────────────────────────────────────

    [Fact]
    public void NotificationChannel_ShouldHaveExpectedValues()
    {
        Enum.GetNames<NotificationChannel>().Should().Contain("InApp");
        Enum.GetNames<NotificationChannel>().Should().Contain("Email");
        Enum.GetNames<NotificationChannel>().Should().Contain("MicrosoftTeams");
        Enum.GetNames<NotificationChannel>().Should().Contain("Sms");
        Enum.GetNames<NotificationChannel>().Should().Contain("PushNotification");
    }

    [Fact]
    public void NotificationSeverity_ShouldHaveExpectedValues()
    {
        Enum.GetNames<NotificationSeverity>().Should().Contain("Info");
        Enum.GetNames<NotificationSeverity>().Should().Contain("Warning");
        Enum.GetNames<NotificationSeverity>().Should().Contain("Critical");
        Enum.GetNames<NotificationSeverity>().Should().Contain("Emergency");
    }

    [Fact]
    public void NotificationCategory_ShouldHaveExpectedValues()
    {
        Enum.GetNames<NotificationCategory>().Should().Contain("Workflow");
        Enum.GetNames<NotificationCategory>().Should().Contain("Approval");
        Enum.GetNames<NotificationCategory>().Should().Contain("Security");
        Enum.GetNames<NotificationCategory>().Should().Contain("Licensing");
        Enum.GetNames<NotificationCategory>().Should().Contain("Contract");
        Enum.GetNames<NotificationCategory>().Should().Contain("Deployment");
        Enum.GetNames<NotificationCategory>().Should().Contain("AccessReview");
    }

    [Fact]
    public void NotificationStatus_ShouldHaveExpectedValues()
    {
        Enum.GetNames<NotificationStatus>().Should().Contain("Pending");
        Enum.GetNames<NotificationStatus>().Should().Contain("Queued");
        Enum.GetNames<NotificationStatus>().Should().Contain("Sent");
        Enum.GetNames<NotificationStatus>().Should().Contain("Delivered");
        Enum.GetNames<NotificationStatus>().Should().Contain("Read");
        Enum.GetNames<NotificationStatus>().Should().Contain("Acknowledged");
        Enum.GetNames<NotificationStatus>().Should().Contain("Failed");
        Enum.GetNames<NotificationStatus>().Should().Contain("Expired");
    }

    // ─── Strongly Typed IDs ──────────────────────────────────────────

    [Fact]
    public void NotificationId_ShouldBeStronglyTyped()
    {
        var guid = Guid.NewGuid();
        var id = new NotificationId(guid);

        id.Value.Should().Be(guid);
        id.Should().Be(new NotificationId(guid));
        id.Should().NotBe(new NotificationId(Guid.NewGuid()));
    }
}
