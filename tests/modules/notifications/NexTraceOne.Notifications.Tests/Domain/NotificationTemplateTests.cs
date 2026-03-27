using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Tests.Domain;

/// <summary>
/// Testes de unidade para a entidade NotificationTemplate.
/// Valida criação, atualização e ciclo de vida de templates persistidos.
/// </summary>
public sealed class NotificationTemplateTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_ValidParameters_ShouldCreateTemplate()
    {
        var template = NotificationTemplate.Create(
            _tenantId,
            "IncidentCreated",
            "Incident Created Template",
            "Incident: {{ServiceName}}",
            "<p>Incident for {{ServiceName}} was created.</p>",
            "Incident for {{ServiceName}} was created.",
            DeliveryChannel.Email,
            "en");

        template.Should().NotBeNull();
        template.Id.Value.Should().NotBeEmpty();
        template.TenantId.Should().Be(_tenantId);
        template.EventType.Should().Be("IncidentCreated");
        template.Name.Should().Be("Incident Created Template");
        template.SubjectTemplate.Should().Be("Incident: {{ServiceName}}");
        template.Channel.Should().Be(DeliveryChannel.Email);
        template.Locale.Should().Be("en");
        template.IsActive.Should().BeTrue();
        template.IsBuiltIn.Should().BeFalse();
        template.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        template.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithNullChannel_ShouldApplyToAllChannels()
    {
        var template = NotificationTemplate.Create(
            _tenantId,
            "ApprovalPending",
            "Approval Template",
            "Approval: {{EntityName}}",
            "Approval required for {{EntityName}}.");

        template.Channel.Should().BeNull();
    }

    [Fact]
    public void CreateBuiltIn_ShouldSetIsBuiltInTrue()
    {
        var template = NotificationTemplate.CreateBuiltIn(
            _tenantId,
            "SecurityAlert",
            "Security Alert Template",
            "Security Alert",
            "Security alert: {{Description}}");

        template.IsBuiltIn.Should().BeTrue();
        template.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Update_ShouldChangeContentAndTimestamp()
    {
        var template = NotificationTemplate.Create(
            _tenantId,
            "IncidentCreated",
            "Old Name",
            "Old Subject",
            "Old Body");

        var before = template.UpdatedAt;

        template.Update("New Name", "New Subject", "New Body", "New plain text");

        template.Name.Should().Be("New Name");
        template.SubjectTemplate.Should().Be("New Subject");
        template.BodyTemplate.Should().Be("New Body");
        template.PlainTextTemplate.Should().Be("New plain text");
        template.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var template = NotificationTemplate.Create(
            _tenantId,
            "IncidentCreated",
            "Template",
            "Subject",
            "Body");

        template.IsActive.Should().BeTrue();
        template.Deactivate();
        template.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_AfterDeactivation_ShouldRestoreIsActive()
    {
        var template = NotificationTemplate.Create(
            _tenantId,
            "IncidentCreated",
            "Template",
            "Subject",
            "Body");

        template.Deactivate();
        template.Activate();

        template.IsActive.Should().BeTrue();
    }
}
