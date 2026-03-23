using System.Text.Json;
using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.ChangeGovernance.Contracts.IntegrationEvents;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Infrastructure.EventHandlers;

/// <summary>
/// Handler para eventos de workflow/aprovação do módulo Change Governance.
/// Gera notificações internas quando aprovações ficam pendentes, são aprovadas, rejeitadas ou expiram.
/// Fase 5: adicionados ApprovalApproved e ApprovalExpiring.
/// </summary>
internal sealed class ApprovalNotificationHandler(
    INotificationModule notificationModule,
    ILogger<ApprovalNotificationHandler> logger)
    : IIntegrationEventHandler<ApprovalPendingIntegrationEvent>,
      IIntegrationEventHandler<WorkflowRejectedIntegrationEvent>,
      IIntegrationEventHandler<ApprovalApprovedIntegrationEvent>,
      IIntegrationEventHandler<ApprovalExpiringIntegrationEvent>
{
    public async Task HandleAsync(ApprovalPendingIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing ApprovalPending notification for workflow {WorkflowId}, stage {StageId}",
            @event.WorkflowId, @event.StageId);

        if (@event.ApproverUserId is null || @event.TenantId is null)
        {
            logger.LogWarning("ApprovalPending event missing ApproverUserId or TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.WorkflowName,
            @event.RequestedBy
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.ApprovalPending,
            Category = nameof(NotificationCategory.Approval),
            Severity = nameof(NotificationSeverity.ActionRequired),
            Title = $"Approval required — {@event.WorkflowName}",
            Message = $"A new approval has been requested by {@event.RequestedBy} for {@event.WorkflowName}. Review and decide.",
            SourceModule = "ChangeGovernance",
            SourceEntityType = "WorkflowStage",
            SourceEntityId = @event.StageId.ToString(),
            ActionUrl = $"/workflows/{@event.WorkflowId}/stages/{@event.StageId}",
            RequiresAction = true,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.ApproverUserId.Value],
            PayloadJson = payload
        }, ct);
    }

    public async Task HandleAsync(WorkflowRejectedIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing WorkflowRejected notification for workflow {WorkflowId}",
            @event.WorkflowId);

        if (@event.OwnerUserId is null || @event.TenantId is null)
        {
            logger.LogWarning("WorkflowRejected event missing OwnerUserId or TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.WorkflowName,
            @event.RejectedBy,
            @event.Reason
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.ApprovalRejected,
            Category = nameof(NotificationCategory.Approval),
            Severity = nameof(NotificationSeverity.Warning),
            Title = $"Rejected — {@event.WorkflowName}",
            Message = $"The approval for {@event.WorkflowName} was rejected by {@event.RejectedBy}. Reason: {@event.Reason}",
            SourceModule = "ChangeGovernance",
            SourceEntityType = "Workflow",
            SourceEntityId = @event.WorkflowId.ToString(),
            ActionUrl = $"/workflows/{@event.WorkflowId}",
            RequiresAction = false,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.OwnerUserId.Value],
            PayloadJson = payload
        }, ct);
    }

    public async Task HandleAsync(ApprovalApprovedIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing ApprovalApproved notification for workflow {WorkflowId}, stage {StageId}",
            @event.WorkflowId, @event.StageId);

        if (@event.OwnerUserId is null || @event.TenantId is null)
        {
            logger.LogWarning("ApprovalApproved event missing OwnerUserId or TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.WorkflowName,
            @event.ApprovedBy
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.ApprovalApproved,
            Category = nameof(NotificationCategory.Approval),
            Severity = nameof(NotificationSeverity.Info),
            Title = $"Approved — {@event.WorkflowName}",
            Message = $"The approval for {@event.WorkflowName} was granted by {@event.ApprovedBy}.",
            SourceModule = "ChangeGovernance",
            SourceEntityType = "WorkflowStage",
            SourceEntityId = @event.StageId.ToString(),
            ActionUrl = $"/workflows/{@event.WorkflowId}/stages/{@event.StageId}",
            RequiresAction = false,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.OwnerUserId.Value],
            PayloadJson = payload
        }, ct);
    }

    public async Task HandleAsync(ApprovalExpiringIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing ApprovalExpiring notification for workflow {WorkflowId}, stage {StageId}",
            @event.WorkflowId, @event.StageId);

        if (@event.ApproverUserId is null || @event.TenantId is null)
        {
            logger.LogWarning("ApprovalExpiring event missing ApproverUserId or TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.WorkflowName,
            ExpiresAt = @event.ExpiresAt.ToString("O")
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.ApprovalExpiring,
            Category = nameof(NotificationCategory.Approval),
            Severity = nameof(NotificationSeverity.Warning),
            Title = $"Approval expiring — {@event.WorkflowName}",
            Message = $"The approval for {@event.WorkflowName} is expiring at {@event.ExpiresAt:g}. Act before the deadline.",
            SourceModule = "ChangeGovernance",
            SourceEntityType = "WorkflowStage",
            SourceEntityId = @event.StageId.ToString(),
            ActionUrl = $"/workflows/{@event.WorkflowId}/stages/{@event.StageId}",
            RequiresAction = true,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.ApproverUserId.Value],
            PayloadJson = payload
        }, ct);
    }
}
