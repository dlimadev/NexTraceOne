using FluentAssertions;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Tests.Domain;

/// <summary>
/// Testes de unidade para a entidade Notification.
/// Valida criação, transições de estado e regras de domínio.
/// </summary>
public sealed class NotificationTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _recipientId = Guid.NewGuid();

    [Fact]
    public void Create_ValidParameters_ShouldCreateNotification()
    {
        var notification = Notification.Create(
            _tenantId, _recipientId,
            "IncidentCreated", NotificationCategory.Incident,
            NotificationSeverity.Critical,
            "Incidente crítico no serviço X",
            "O serviço X está com latência acima de 5s no ambiente Production.",
            "OperationalIntelligence",
            sourceEntityType: "Incident",
            sourceEntityId: Guid.NewGuid().ToString());

        notification.Should().NotBeNull();
        notification.Id.Value.Should().NotBeEmpty();
        notification.TenantId.Should().Be(_tenantId);
        notification.RecipientUserId.Should().Be(_recipientId);
        notification.EventType.Should().Be("IncidentCreated");
        notification.Category.Should().Be(NotificationCategory.Incident);
        notification.Severity.Should().Be(NotificationSeverity.Critical);
        notification.Status.Should().Be(NotificationStatus.Unread);
        notification.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        notification.ReadAt.Should().BeNull();
        notification.AcknowledgedAt.Should().BeNull();
        notification.ArchivedAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithRequiresAction_ShouldSetFlag()
    {
        var notification = Notification.Create(
            _tenantId, _recipientId,
            "ApprovalPending", NotificationCategory.Approval,
            NotificationSeverity.ActionRequired,
            "Aprovação pendente", "Release v1.2.0 aguarda aprovação.",
            "ChangeGovernance",
            requiresAction: true);

        notification.RequiresAction.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldRaiseDomainEvent()
    {
        var notification = Notification.Create(
            _tenantId, _recipientId,
            "IncidentCreated", NotificationCategory.Incident,
            NotificationSeverity.Warning,
            "Alerta de degradação", "Serviço Y com erros intermitentes.",
            "OperationalIntelligence");

        notification.DomainEvents.Should().HaveCount(1);
        notification.DomainEvents[0].Should().BeOfType<NexTraceOne.Notifications.Domain.Events.NotificationCreatedEvent>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyEventType_ShouldThrow(string? eventType)
    {
        var act = () => Notification.Create(
            _tenantId, _recipientId,
            eventType!, NotificationCategory.Incident,
            NotificationSeverity.Info,
            "Title", "Message", "Module");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyTitle_ShouldThrow(string? title)
    {
        var act = () => Notification.Create(
            _tenantId, _recipientId,
            "Event", NotificationCategory.Incident,
            NotificationSeverity.Info,
            title!, "Message", "Module");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyMessage_ShouldThrow(string? message)
    {
        var act = () => Notification.Create(
            _tenantId, _recipientId,
            "Event", NotificationCategory.Incident,
            NotificationSeverity.Info,
            "Title", message!, "Module");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptySourceModule_ShouldThrow(string? sourceModule)
    {
        var act = () => Notification.Create(
            _tenantId, _recipientId,
            "Event", NotificationCategory.Incident,
            NotificationSeverity.Info,
            "Title", "Message", sourceModule!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkAsRead_FromUnread_ShouldTransition()
    {
        var notification = CreateTestNotification();

        notification.MarkAsRead();

        notification.Status.Should().Be(NotificationStatus.Read);
        notification.ReadAt.Should().NotBeNull();
        notification.DomainEvents.Should().HaveCount(2); // Created + Read
    }

    [Fact]
    public void MarkAsRead_FromRead_ShouldNotChangeState()
    {
        var notification = CreateTestNotification();
        notification.MarkAsRead();
        var readAt = notification.ReadAt;

        notification.MarkAsRead(); // idempotent

        notification.Status.Should().Be(NotificationStatus.Read);
        notification.ReadAt.Should().Be(readAt);
    }

    [Fact]
    public void MarkAsUnread_FromRead_ShouldTransition()
    {
        var notification = CreateTestNotification();
        notification.MarkAsRead();

        notification.MarkAsUnread();

        notification.Status.Should().Be(NotificationStatus.Unread);
        notification.ReadAt.Should().BeNull();
    }

    [Fact]
    public void MarkAsUnread_FromUnread_ShouldNotChange()
    {
        var notification = CreateTestNotification();

        notification.MarkAsUnread();

        notification.Status.Should().Be(NotificationStatus.Unread);
    }

    [Fact]
    public void Acknowledge_FromUnread_ShouldTransition()
    {
        var notification = CreateTestNotification();

        notification.Acknowledge();

        notification.Status.Should().Be(NotificationStatus.Acknowledged);
        notification.AcknowledgedAt.Should().NotBeNull();
        notification.ReadAt.Should().NotBeNull(); // implicitly read
    }

    [Fact]
    public void Acknowledge_FromRead_ShouldTransition()
    {
        var notification = CreateTestNotification();
        notification.MarkAsRead();

        notification.Acknowledge();

        notification.Status.Should().Be(NotificationStatus.Acknowledged);
        notification.AcknowledgedAt.Should().NotBeNull();
    }

    [Fact]
    public void Acknowledge_FromArchived_ShouldNotChange()
    {
        var notification = CreateTestNotification();
        notification.Archive();

        notification.Acknowledge();

        notification.Status.Should().Be(NotificationStatus.Archived);
    }

    [Fact]
    public void Archive_FromUnread_ShouldTransition()
    {
        var notification = CreateTestNotification();

        notification.Archive();

        notification.Status.Should().Be(NotificationStatus.Archived);
        notification.ArchivedAt.Should().NotBeNull();
    }

    [Fact]
    public void Archive_FromArchived_ShouldBeIdempotent()
    {
        var notification = CreateTestNotification();
        notification.Archive();
        var archivedAt = notification.ArchivedAt;

        notification.Archive();

        notification.Status.Should().Be(NotificationStatus.Archived);
        notification.ArchivedAt.Should().Be(archivedAt);
    }

    [Fact]
    public void Dismiss_FromUnread_ShouldTransition()
    {
        var notification = CreateTestNotification();

        notification.Dismiss();

        notification.Status.Should().Be(NotificationStatus.Dismissed);
    }

    [Fact]
    public void Dismiss_FromRead_ShouldTransition()
    {
        var notification = CreateTestNotification();
        notification.MarkAsRead();

        notification.Dismiss();

        notification.Status.Should().Be(NotificationStatus.Dismissed);
    }

    [Fact]
    public void Dismiss_FromArchived_ShouldNotChange()
    {
        var notification = CreateTestNotification();
        notification.Archive();

        notification.Dismiss();

        notification.Status.Should().Be(NotificationStatus.Archived);
    }

    [Fact]
    public void IsExpired_WithFutureExpiry_ShouldReturnFalse()
    {
        var notification = Notification.Create(
            _tenantId, _recipientId,
            "Event", NotificationCategory.Informational,
            NotificationSeverity.Info,
            "Title", "Message", "Module",
            expiresAt: DateTimeOffset.UtcNow.AddHours(1));

        notification.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WithPastExpiry_ShouldReturnTrue()
    {
        var notification = Notification.Create(
            _tenantId, _recipientId,
            "Event", NotificationCategory.Informational,
            NotificationSeverity.Info,
            "Title", "Message", "Module",
            expiresAt: DateTimeOffset.UtcNow.AddHours(-1));

        notification.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WithNoExpiry_ShouldReturnFalse()
    {
        var notification = CreateTestNotification();

        notification.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void Create_WithAllOptionalFields_ShouldSetAllValues()
    {
        var environmentId = Guid.NewGuid();
        var entityId = Guid.NewGuid().ToString();
        var payload = """{"key":"value"}""";

        var notification = Notification.Create(
            _tenantId, _recipientId,
            "ContractPublished", NotificationCategory.Contract,
            NotificationSeverity.Info,
            "Contrato publicado", "API Orders v2.0 publicada no portal.",
            "Catalog",
            sourceEntityType: "Contract",
            sourceEntityId: entityId,
            environmentId: environmentId,
            actionUrl: "/contracts/orders-api/v2",
            requiresAction: false,
            payloadJson: payload,
            expiresAt: DateTimeOffset.UtcNow.AddDays(30));

        notification.SourceEntityType.Should().Be("Contract");
        notification.SourceEntityId.Should().Be(entityId);
        notification.EnvironmentId.Should().Be(environmentId);
        notification.ActionUrl.Should().Be("/contracts/orders-api/v2");
        notification.PayloadJson.Should().Be(payload);
        notification.ExpiresAt.Should().NotBeNull();
    }

    private Notification CreateTestNotification() =>
        Notification.Create(
            _tenantId, _recipientId,
            "TestEvent", NotificationCategory.Informational,
            NotificationSeverity.Info,
            "Test notification", "Test message body.",
            "TestModule");
}
