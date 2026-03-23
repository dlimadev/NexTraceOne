using Microsoft.Extensions.Logging;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.Engine;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Tests.Engine;

public sealed class NotificationOrchestratorTests
{
    private readonly INotificationStore _store = Substitute.For<INotificationStore>();
    private readonly INotificationTemplateResolver _templateResolver = new NotificationTemplateResolver();
    private readonly INotificationDeduplicationService _dedup = Substitute.For<INotificationDeduplicationService>();
    private readonly ILogger<NotificationOrchestrator> _logger = Substitute.For<ILogger<NotificationOrchestrator>>();
    private readonly NotificationOrchestrator _orchestrator;

    public NotificationOrchestratorTests()
    {
        _dedup.IsDuplicateAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _orchestrator = new NotificationOrchestrator(_store, _templateResolver, _dedup, null, _logger);
    }

    [Fact]
    public async Task ProcessAsync_WithValidRequest_CreatesNotification()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var request = new NotificationRequest
        {
            EventType = NotificationType.IncidentCreated,
            Category = nameof(NotificationCategory.Incident),
            Severity = nameof(NotificationSeverity.Critical),
            Title = "Incident created — payments-api",
            Message = "A new critical incident for payments-api.",
            SourceModule = "OperationalIntelligence",
            SourceEntityType = "Incident",
            SourceEntityId = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            RecipientUserIds = [userId]
        };

        var result = await _orchestrator.ProcessAsync(request);

        result.Success.Should().BeTrue();
        result.NotificationIds.Should().HaveCount(1);
        await _store.Received(1).AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        await _store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithMultipleRecipients_CreatesOnePerRecipient()
    {
        var tenantId = Guid.NewGuid();
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var user3 = Guid.NewGuid();

        var request = new NotificationRequest
        {
            EventType = NotificationType.ApprovalPending,
            Category = nameof(NotificationCategory.Approval),
            Severity = nameof(NotificationSeverity.ActionRequired),
            Title = "Approval required",
            Message = "Review and decide.",
            SourceModule = "ChangeGovernance",
            TenantId = tenantId,
            RecipientUserIds = [user1, user2, user3]
        };

        var result = await _orchestrator.ProcessAsync(request);

        result.Success.Should().BeTrue();
        result.NotificationIds.Should().HaveCount(3);
        await _store.Received(3).AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_MissingEventType_ReturnsFail()
    {
        var request = new NotificationRequest
        {
            EventType = "",
            Category = "Incident",
            Severity = "Critical",
            Title = "Title",
            Message = "Message",
            SourceModule = "Module",
            TenantId = Guid.NewGuid(),
            RecipientUserIds = [Guid.NewGuid()]
        };

        var result = await _orchestrator.ProcessAsync(request);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("EventType");
    }

    [Fact]
    public async Task ProcessAsync_MissingTenantId_ReturnsFail()
    {
        var request = new NotificationRequest
        {
            EventType = NotificationType.IncidentCreated,
            Category = "Incident",
            Severity = "Critical",
            Title = "Title",
            Message = "Message",
            SourceModule = "Module",
            TenantId = null,
            RecipientUserIds = [Guid.NewGuid()]
        };

        var result = await _orchestrator.ProcessAsync(request);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("TenantId");
    }

    [Fact]
    public async Task ProcessAsync_MissingSourceModule_ReturnsFail()
    {
        var request = new NotificationRequest
        {
            EventType = NotificationType.IncidentCreated,
            Category = "Incident",
            Severity = "Critical",
            Title = "Title",
            Message = "Message",
            SourceModule = "",
            TenantId = Guid.NewGuid(),
            RecipientUserIds = [Guid.NewGuid()]
        };

        var result = await _orchestrator.ProcessAsync(request);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("SourceModule");
    }

    [Fact]
    public async Task ProcessAsync_NoRecipients_ReturnsFail()
    {
        var request = new NotificationRequest
        {
            EventType = NotificationType.IncidentCreated,
            Category = "Incident",
            Severity = "Critical",
            Title = "Title",
            Message = "Message",
            SourceModule = "Module",
            TenantId = Guid.NewGuid(),
            RecipientUserIds = []
        };

        var result = await _orchestrator.ProcessAsync(request);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("recipients");
    }

    [Fact]
    public async Task ProcessAsync_DuplicateDetected_SkipsCreation()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid().ToString();

        _dedup.IsDuplicateAsync(tenantId, userId, NotificationType.IncidentCreated, entityId, 5, Arg.Any<CancellationToken>())
            .Returns(true);

        var request = new NotificationRequest
        {
            EventType = NotificationType.IncidentCreated,
            Category = "Incident",
            Severity = "Critical",
            Title = "Incident created",
            Message = "A new incident.",
            SourceModule = "OperationalIntelligence",
            SourceEntityId = entityId,
            TenantId = tenantId,
            RecipientUserIds = [userId]
        };

        var result = await _orchestrator.ProcessAsync(request);

        result.Success.Should().BeTrue();
        result.NotificationIds.Should().BeEmpty();
        await _store.DidNotReceive().AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_PreservesSourceContext()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid().ToString();
        Notification? capturedNotification = null;

        _store.AddAsync(Arg.Do<Notification>(n => capturedNotification = n), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var request = new NotificationRequest
        {
            EventType = NotificationType.ApprovalPending,
            Category = nameof(NotificationCategory.Approval),
            Severity = nameof(NotificationSeverity.ActionRequired),
            Title = "Approval required — Release v2.0",
            Message = "Review the release.",
            SourceModule = "ChangeGovernance",
            SourceEntityType = "WorkflowStage",
            SourceEntityId = entityId,
            ActionUrl = $"/workflows/{entityId}",
            TenantId = tenantId,
            RecipientUserIds = [userId],
            PayloadJson = """{"WorkflowName":"Release v2.0"}"""
        };

        await _orchestrator.ProcessAsync(request);

        capturedNotification.Should().NotBeNull();
        capturedNotification!.SourceModule.Should().Be("ChangeGovernance");
        capturedNotification.SourceEntityType.Should().Be("WorkflowStage");
        capturedNotification.SourceEntityId.Should().Be(entityId);
        capturedNotification.ActionUrl.Should().Contain(entityId);
        capturedNotification.TenantId.Should().Be(tenantId);
        capturedNotification.RecipientUserId.Should().Be(userId);
        capturedNotification.EventType.Should().Be(NotificationType.ApprovalPending);
        capturedNotification.PayloadJson.Should().Contain("Release v2.0");
    }

    [Fact]
    public async Task ProcessAsync_WithPayloadJson_ExtractsParametersForTemplate()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        Notification? capturedNotification = null;

        _store.AddAsync(Arg.Do<Notification>(n => capturedNotification = n), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var request = new NotificationRequest
        {
            EventType = NotificationType.BudgetExceeded,
            Category = "",
            Severity = "",
            Title = "",  // Empty to use template
            Message = "", // Empty to use template
            SourceModule = "OperationalIntelligence",
            TenantId = tenantId,
            RecipientUserIds = [userId],
            PayloadJson = """{"ServiceName":"data-pipeline","ExpectedCost":"$500","ActualCost":"$1200"}"""
        };

        await _orchestrator.ProcessAsync(request);

        capturedNotification.Should().NotBeNull();
        capturedNotification!.Title.Should().Contain("data-pipeline");
        capturedNotification.Message.Should().Contain("$500");
        capturedNotification.Category.Should().Be(NotificationCategory.FinOps);
    }

    [Fact]
    public async Task ProcessAsync_DuplicateRecipients_DeduplicatesUsers()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var request = new NotificationRequest
        {
            EventType = NotificationType.IncidentCreated,
            Category = "Incident",
            Severity = "Critical",
            Title = "Incident",
            Message = "A critical incident.",
            SourceModule = "OperationalIntelligence",
            TenantId = tenantId,
            RecipientUserIds = [userId, userId, userId] // Same user 3x
        };

        var result = await _orchestrator.ProcessAsync(request);

        result.Success.Should().BeTrue();
        result.NotificationIds.Should().HaveCount(1); // Only 1 notification created
    }

    [Fact]
    public async Task ProcessAsync_EmptyGuidRecipients_AreFiltered()
    {
        var tenantId = Guid.NewGuid();
        var validUser = Guid.NewGuid();

        var request = new NotificationRequest
        {
            EventType = NotificationType.IncidentCreated,
            Category = "Incident",
            Severity = "Critical",
            Title = "Incident",
            Message = "Incident.",
            SourceModule = "OperationalIntelligence",
            TenantId = tenantId,
            RecipientUserIds = [Guid.Empty, validUser, Guid.Empty]
        };

        var result = await _orchestrator.ProcessAsync(request);

        result.Success.Should().BeTrue();
        result.NotificationIds.Should().HaveCount(1);
    }

    [Fact]
    public async Task ProcessAsync_NullRequest_ThrowsArgumentNull()
    {
        var act = () => _orchestrator.ProcessAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
