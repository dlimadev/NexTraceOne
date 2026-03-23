using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.ChangeGovernance.Contracts.IntegrationEvents;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Infrastructure.EventHandlers;

namespace NexTraceOne.Notifications.Tests.Engine.EventHandlers;

public sealed class ApprovalNotificationHandlerTests
{
    private readonly List<NotificationRequest> _captured = [];
    private readonly INotificationModule _module = Substitute.For<INotificationModule>();
    private readonly ApprovalNotificationHandler _handler;

    public ApprovalNotificationHandlerTests()
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

    [Fact]
    public async Task HandleAsync_ApprovalPending_SubmitsToApprover()
    {
        var approverUserId = Guid.NewGuid();
        var @event = new ApprovalPendingIntegrationEvent(
            WorkflowId: Guid.NewGuid(),
            StageId: Guid.NewGuid(),
            WorkflowName: "Release v2.0",
            RequestedBy: "john.doe",
            ApproverUserId: approverUserId) { TenantId = Guid.NewGuid() };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        _captured[0].EventType.Should().Be(NotificationType.ApprovalPending);
        _captured[0].SourceModule.Should().Be("ChangeGovernance");
        _captured[0].RecipientUserIds.Should().Contain(approverUserId);
        _captured[0].RequiresAction.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ApprovalPending_MissingApprover_Skips()
    {
        var @event = new ApprovalPendingIntegrationEvent(
            WorkflowId: Guid.NewGuid(),
            StageId: Guid.NewGuid(),
            WorkflowName: "Test",
            RequestedBy: "user",
            ApproverUserId: null) { TenantId = Guid.NewGuid() };

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WorkflowRejected_SubmitsToOwner()
    {
        var ownerUserId = Guid.NewGuid();
        var @event = new WorkflowRejectedIntegrationEvent(
            WorkflowId: Guid.NewGuid(),
            StageId: Guid.NewGuid(),
            WorkflowName: "Release v2.0",
            RejectedBy: "security.lead",
            Reason: "Missing tests",
            OwnerUserId: ownerUserId) { TenantId = Guid.NewGuid() };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        _captured[0].EventType.Should().Be(NotificationType.ApprovalRejected);
        _captured[0].RecipientUserIds.Should().Contain(ownerUserId);
        _captured[0].Title.Should().Contain("Release v2.0");
        _captured[0].Message.Should().Contain("Missing tests");
    }

    [Fact]
    public async Task HandleAsync_WorkflowRejected_MissingOwner_Skips()
    {
        var @event = new WorkflowRejectedIntegrationEvent(
            WorkflowId: Guid.NewGuid(),
            StageId: Guid.NewGuid(),
            WorkflowName: "Test",
            RejectedBy: "user",
            Reason: "Reason",
            OwnerUserId: null) { TenantId = Guid.NewGuid() };

        await _handler.HandleAsync(@event);

        _captured.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ApprovalPending_IncludesDeepLink()
    {
        var workflowId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var @event = new ApprovalPendingIntegrationEvent(
            WorkflowId: workflowId,
            StageId: stageId,
            WorkflowName: "Release",
            RequestedBy: "user",
            ApproverUserId: Guid.NewGuid()) { TenantId = Guid.NewGuid() };

        await _handler.HandleAsync(@event);

        _captured.Should().HaveCount(1);
        _captured[0].ActionUrl.Should().Be($"/workflows/{workflowId}/stages/{stageId}");
    }
}
