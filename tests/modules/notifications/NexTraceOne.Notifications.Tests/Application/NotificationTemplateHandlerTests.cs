using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.Features.ListNotificationTemplates;
using NexTraceOne.Notifications.Application.Features.UpsertNotificationTemplate;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Tests.Application;

/// <summary>
/// Testes de unidade para os handlers de templates de notificação (P7.1).
/// </summary>
public sealed class NotificationTemplateHandlerTests
{
    private readonly INotificationTemplateStore _store = Substitute.For<INotificationTemplateStore>();
    private readonly ICurrentTenant _tenant = Substitute.For<ICurrentTenant>();
    private readonly Guid _tenantId = Guid.NewGuid();

    public NotificationTemplateHandlerTests()
    {
        _tenant.Id.Returns(_tenantId);
    }

    // ── ListNotificationTemplates ──────────────────────────────────────────

    [Fact]
    public async Task ListTemplates_ReturnsAllTemplatesForTenant()
    {
        var templates = new List<NotificationTemplate>
        {
            NotificationTemplate.Create(_tenantId, "IncidentCreated", "T1", "Subject1", "Body1"),
            NotificationTemplate.Create(_tenantId, "ApprovalPending", "T2", "Subject2", "Body2"),
        };

        _store.ListAsync(_tenantId, null, null, null, Arg.Any<CancellationToken>())
            .Returns(templates.AsReadOnly());

        var handler = new ListNotificationTemplates.Handler(_store, _tenant);
        var result = await handler.Handle(new ListNotificationTemplates.Query(null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListTemplates_WithEventTypeFilter_PassesFilterToStore()
    {
        _store.ListAsync(_tenantId, "IncidentCreated", null, null, Arg.Any<CancellationToken>())
            .Returns(new List<NotificationTemplate>().AsReadOnly());

        var handler = new ListNotificationTemplates.Handler(_store, _tenant);
        await handler.Handle(new ListNotificationTemplates.Query("IncidentCreated", null, null), CancellationToken.None);

        await _store.Received(1).ListAsync(_tenantId, "IncidentCreated", null, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListTemplates_WithInvalidChannel_ShouldFailValidation()
    {
        var validator = new ListNotificationTemplates.Validator();
        var result = validator.Validate(new ListNotificationTemplates.Query(null, "InvalidChannel", null));
        result.IsValid.Should().BeFalse();
    }

    // ── UpsertNotificationTemplate (Create) ────────────────────────────────

    [Fact]
    public async Task UpsertTemplate_Create_ShouldAddTemplateAndReturnCreatedTrue()
    {
        NotificationTemplate? captured = null;
        _store.When(x => x.AddAsync(Arg.Any<NotificationTemplate>(), Arg.Any<CancellationToken>()))
            .Do(ci => captured = ci.Arg<NotificationTemplate>());

        var handler = new UpsertNotificationTemplate.Handler(_store, _tenant);
        var command = new UpsertNotificationTemplate.Command(
            Id: null,
            EventType: "IncidentCreated",
            Name: "My Template",
            SubjectTemplate: "Subject",
            BodyTemplate: "Body",
            PlainTextTemplate: null,
            Channel: "Email",
            Locale: "en");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Created.Should().BeTrue();
        await _store.Received(1).AddAsync(Arg.Any<NotificationTemplate>(), Arg.Any<CancellationToken>());
        await _store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpsertTemplate_Update_ShouldUpdateExistingTemplate()
    {
        var templateId = Guid.NewGuid();
        var existing = NotificationTemplate.Create(_tenantId, "IncidentCreated", "Old", "OldSubject", "OldBody");

        _store.GetByIdAsync(Arg.Any<NotificationTemplateId>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var handler = new UpsertNotificationTemplate.Handler(_store, _tenant);
        var command = new UpsertNotificationTemplate.Command(
            Id: templateId,
            EventType: "IncidentCreated",
            Name: "New Name",
            SubjectTemplate: "New Subject",
            BodyTemplate: "New Body",
            PlainTextTemplate: null,
            Channel: null,
            Locale: "en");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Created.Should().BeFalse();
        existing.Name.Should().Be("New Name");
        existing.SubjectTemplate.Should().Be("New Subject");
    }

    [Fact]
    public async Task UpsertTemplate_Update_NotFound_ShouldReturnNotFoundError()
    {
        _store.GetByIdAsync(Arg.Any<NotificationTemplateId>(), Arg.Any<CancellationToken>())
            .Returns((NotificationTemplate?)null);

        var handler = new UpsertNotificationTemplate.Handler(_store, _tenant);
        var command = new UpsertNotificationTemplate.Command(
            Id: Guid.NewGuid(),
            EventType: "IncidentCreated",
            Name: "Name",
            SubjectTemplate: "Subject",
            BodyTemplate: "Body",
            PlainTextTemplate: null,
            Channel: null,
            Locale: "en");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotificationTemplate.NotFound");
    }

    [Fact]
    public async Task UpsertTemplate_Update_WrongTenant_ShouldReturnForbiddenError()
    {
        var differentTenantId = Guid.NewGuid();
        var existing = NotificationTemplate.Create(differentTenantId, "IncidentCreated", "Old", "OldSubject", "OldBody");

        _store.GetByIdAsync(Arg.Any<NotificationTemplateId>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var handler = new UpsertNotificationTemplate.Handler(_store, _tenant);
        var command = new UpsertNotificationTemplate.Command(
            Id: Guid.NewGuid(),
            EventType: "IncidentCreated",
            Name: "Name",
            SubjectTemplate: "Subject",
            BodyTemplate: "Body",
            PlainTextTemplate: null,
            Channel: null,
            Locale: "en");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("NotificationTemplate.Forbidden");
    }
}
