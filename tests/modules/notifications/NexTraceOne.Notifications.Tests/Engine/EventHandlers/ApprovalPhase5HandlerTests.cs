using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.ChangeGovernance.Contracts.IntegrationEvents;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Infrastructure.EventHandlers;

namespace NexTraceOne.Notifications.Tests.Engine.EventHandlers;

/// <summary>
/// Testes para os handlers de aprovação expandidos na Fase 5:
/// ApprovalApproved e ApprovalExpiring.
/// </summary>
public sealed class ApprovalPhase5HandlerTests
{
    private readonly INotificationModule _module = Substitute.For<INotificationModule>();
    private readonly List<NotificationRequest> _captured = [];
    private readonly ApprovalNotificationHandler _handler;

    public ApprovalPhase5HandlerTests()
    {
        _module.SubmitAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                _captured.Add(ci.ArgAt<NotificationRequest>(0));
                return new NotificationResult(true) { NotificationIds = [Guid.NewGuid()] };
            });
        _handler = new ApprovalNotificationHandler(
            _module,
            NullLoggerFactory.Instance.CreateLogger<ApprovalNotificationHandler>());
    }

    // ── ApprovalApproved ──

    [Fact]
    public async Task HandleAsync_ApprovalApproved_SubmitsNotification()
    {
        var tenantId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var @event = new ApprovalApprovedIntegrationEvent(
            WorkflowId: workflowId,
            StageId: stageId,
            WorkflowName: "Release v2.0",
            ApprovedBy: "manager@corp.com",
            OwnerUserId: ownerUserId) { TenantId = tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        var r = _captured[0];
        r.EventType.Should().Be(NotificationType.ApprovalApproved);
        r.Category.Should().Be(nameof(NotificationCategory.Approval));
        r.Severity.Should().Be(nameof(NotificationSeverity.Info));
        r.SourceModule.Should().Be("ChangeGovernance");
        r.SourceEntityType.Should().Be("WorkflowStage");
        r.SourceEntityId.Should().Be(stageId.ToString());
        r.ActionUrl.Should().Be($"/workflows/{workflowId}/stages/{stageId}");
        r.RequiresAction.Should().BeFalse();
        r.TenantId.Should().Be(tenantId);
        r.RecipientUserIds.Should().Contain(ownerUserId);
        r.Title.Should().Contain("Release v2.0");
        r.Message.Should().Contain("manager@corp.com");
    }

    [Fact]
    public async Task HandleAsync_ApprovalApproved_MissingOwner_Skips()
    {
        var tenantId = Guid.NewGuid();
        var @event = new ApprovalApprovedIntegrationEvent(
            WorkflowId: Guid.NewGuid(),
            StageId: Guid.NewGuid(),
            WorkflowName: "Test",
            ApprovedBy: "admin",
            OwnerUserId: null) { TenantId = tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ApprovalApproved_MissingTenant_Skips()
    {
        var @event = new ApprovalApprovedIntegrationEvent(
            WorkflowId: Guid.NewGuid(),
            StageId: Guid.NewGuid(),
            WorkflowName: "Test",
            ApprovedBy: "admin",
            OwnerUserId: Guid.NewGuid()) { TenantId = null };

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }

    // ── ApprovalExpiring ──

    [Fact]
    public async Task HandleAsync_ApprovalExpiring_SubmitsNotification()
    {
        var tenantId = Guid.NewGuid();
        var approverUserId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(2);
        var @event = new ApprovalExpiringIntegrationEvent(
            WorkflowId: workflowId,
            StageId: stageId,
            WorkflowName: "Hotfix Deploy",
            ExpiresAt: expiresAt,
            ApproverUserId: approverUserId) { TenantId = tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        var r = _captured[0];
        r.EventType.Should().Be(NotificationType.ApprovalExpiring);
        r.Category.Should().Be(nameof(NotificationCategory.Approval));
        r.Severity.Should().Be(nameof(NotificationSeverity.Warning));
        r.SourceEntityType.Should().Be("WorkflowStage");
        r.ActionUrl.Should().Be($"/workflows/{workflowId}/stages/{stageId}");
        r.RequiresAction.Should().BeTrue();
        r.RecipientUserIds.Should().Contain(approverUserId);
        r.Title.Should().Contain("Hotfix Deploy");
        r.Message.Should().Contain("expiring");
    }

    [Fact]
    public async Task HandleAsync_ApprovalExpiring_MissingApprover_Skips()
    {
        var tenantId = Guid.NewGuid();
        var @event = new ApprovalExpiringIntegrationEvent(
            WorkflowId: Guid.NewGuid(),
            StageId: Guid.NewGuid(),
            WorkflowName: "Test",
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(1),
            ApproverUserId: null) { TenantId = tenantId };

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }
}
