using System.Text.Json;
using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.ChangeGovernance.Contracts.IntegrationEvents;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Infrastructure.EventHandlers;

/// <summary>
/// Handler para eventos de Change Intelligence do módulo Change Governance.
/// Gera notificações internas quando promoções são concluídas ou bloqueadas,
/// rollbacks são acionados, deploys completam, change confidence score é calculado,
/// blast radius é alto ou a verificação pós-mudança falha.
/// </summary>
internal sealed class ChangeIntelligenceNotificationHandler(
    INotificationModule notificationModule,
    ILogger<ChangeIntelligenceNotificationHandler> logger)
    : IIntegrationEventHandler<PromotionCompletedIntegrationEvent>,
      IIntegrationEventHandler<PromotionBlockedIntegrationEvent>,
      IIntegrationEventHandler<RollbackTriggeredIntegrationEvent>,
      IIntegrationEventHandler<DeploymentCompletedIntegrationEvent>,
      IIntegrationEventHandler<ChangeConfidenceScoredIntegrationEvent>,
      IIntegrationEventHandler<BlastRadiusHighIntegrationEvent>,
      IIntegrationEventHandler<PostChangeVerificationFailedIntegrationEvent>
{
    public async Task HandleAsync(PromotionCompletedIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing PromotionCompleted notification for promotion {PromotionId}, service {ServiceName}",
            @event.PromotionId, @event.ServiceName);

        if (@event.OwnerUserId is null || @event.TenantId is null)
        {
            logger.LogWarning("PromotionCompleted event missing OwnerUserId or TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.ServiceName,
            @event.TargetEnvironment
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.PromotionCompleted,
            Category = nameof(NotificationCategory.Change),
            Severity = nameof(NotificationSeverity.Info),
            Title = $"Promotion completed — {@event.ServiceName}",
            Message = $"Service {@event.ServiceName} was successfully promoted to {@event.TargetEnvironment}.",
            SourceModule = "ChangeGovernance",
            SourceEntityType = "Promotion",
            SourceEntityId = @event.PromotionId.ToString(),
            ActionUrl = $"/changes/promotions/{@event.PromotionId}",
            RequiresAction = false,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.OwnerUserId.Value],
            PayloadJson = payload
        }, ct);
    }

    public async Task HandleAsync(PromotionBlockedIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing PromotionBlocked notification for promotion {PromotionId}, service {ServiceName}",
            @event.PromotionId, @event.ServiceName);

        if (@event.OwnerUserId is null || @event.TenantId is null)
        {
            logger.LogWarning("PromotionBlocked event missing OwnerUserId or TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.ServiceName,
            @event.TargetEnvironment,
            @event.Reason
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.PromotionBlocked,
            Category = nameof(NotificationCategory.Change),
            Severity = nameof(NotificationSeverity.Warning),
            Title = $"Promotion blocked — {@event.ServiceName}",
            Message = $"Promotion of service {@event.ServiceName} to {@event.TargetEnvironment} was blocked. Reason: {@event.Reason}",
            SourceModule = "ChangeGovernance",
            SourceEntityType = "Promotion",
            SourceEntityId = @event.PromotionId.ToString(),
            ActionUrl = $"/changes/promotions/{@event.PromotionId}",
            RequiresAction = true,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.OwnerUserId.Value],
            PayloadJson = payload
        }, ct);
    }

    public async Task HandleAsync(RollbackTriggeredIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing RollbackTriggered notification for change {ChangeId}, service {ServiceName}",
            @event.ChangeId, @event.ServiceName);

        if (@event.OwnerUserId is null || @event.TenantId is null)
        {
            logger.LogWarning("RollbackTriggered event missing OwnerUserId or TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.ServiceName,
            @event.EnvironmentName,
            @event.Reason
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.RollbackTriggered,
            Category = nameof(NotificationCategory.Change),
            Severity = nameof(NotificationSeverity.Critical),
            Title = $"Rollback triggered — {@event.ServiceName}",
            Message = $"A rollback was triggered for service {@event.ServiceName} in {@event.EnvironmentName}. Reason: {@event.Reason}",
            SourceModule = "ChangeGovernance",
            SourceEntityType = "Change",
            SourceEntityId = @event.ChangeId.ToString(),
            ActionUrl = $"/changes/{@event.ChangeId}",
            RequiresAction = true,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.OwnerUserId.Value],
            PayloadJson = payload
        }, ct);
    }

    public async Task HandleAsync(DeploymentCompletedIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing DeploymentCompleted notification for change {ChangeId}, service {ServiceName}, success={IsSuccess}",
            @event.ChangeId, @event.ServiceName, @event.IsSuccess);

        if (@event.OwnerUserId is null || @event.TenantId is null)
        {
            logger.LogWarning("DeploymentCompleted event missing OwnerUserId or TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.ServiceName,
            @event.EnvironmentName,
            @event.IsSuccess,
            @event.FailureReason
        });

        var severity = @event.IsSuccess
            ? nameof(NotificationSeverity.Info)
            : nameof(NotificationSeverity.Warning);

        var title = @event.IsSuccess
            ? $"Deployment completed — {@event.ServiceName}"
            : $"Deployment failed — {@event.ServiceName}";

        var message = @event.IsSuccess
            ? $"Service {@event.ServiceName} was successfully deployed to {@event.EnvironmentName}."
            : $"Deployment of service {@event.ServiceName} to {@event.EnvironmentName} failed. Reason: {@event.FailureReason}";

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.DeploymentCompleted,
            Category = nameof(NotificationCategory.Change),
            Severity = severity,
            Title = title,
            Message = message,
            SourceModule = "ChangeGovernance",
            SourceEntityType = "Change",
            SourceEntityId = @event.ChangeId.ToString(),
            ActionUrl = $"/changes/{@event.ChangeId}",
            RequiresAction = !@event.IsSuccess,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.OwnerUserId.Value],
            PayloadJson = payload
        }, ct);
    }

    public async Task HandleAsync(ChangeConfidenceScoredIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing ChangeConfidenceScored notification for change {ChangeId}, service {ServiceName}, score={ConfidenceScore}",
            @event.ChangeId, @event.ServiceName, @event.ConfidenceScore);

        if (@event.OwnerUserId is null || @event.TenantId is null)
        {
            logger.LogWarning("ChangeConfidenceScored event missing OwnerUserId or TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.ServiceName,
            @event.EnvironmentName,
            ConfidenceScore = (double)@event.ConfidenceScore
        });

        // ConfidenceScore is expected in decimal format (0.0–1.0), e.g. 0.42 = 42%.
        var scorePercent = (double)@event.ConfidenceScore * 100;

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.ChangeConfidenceScored,
            Category = nameof(NotificationCategory.Change),
            Severity = nameof(NotificationSeverity.Warning),
            Title = $"Low change confidence — {@event.ServiceName}",
            Message = $"Change confidence for service {@event.ServiceName} in {@event.EnvironmentName} scored {scorePercent:F0}%. Review before promoting.",
            SourceModule = "ChangeGovernance",
            SourceEntityType = "Change",
            SourceEntityId = @event.ChangeId.ToString(),
            ActionUrl = $"/changes/{@event.ChangeId}/confidence",
            RequiresAction = true,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.OwnerUserId.Value],
            PayloadJson = payload
        }, ct);
    }

    public async Task HandleAsync(BlastRadiusHighIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing BlastRadiusHigh notification for change {ChangeId}, service {ServiceName}, affectedServices={AffectedServiceCount}",
            @event.ChangeId, @event.ServiceName, @event.AffectedServiceCount);

        if (@event.OwnerUserId is null || @event.TenantId is null)
        {
            logger.LogWarning("BlastRadiusHigh event missing OwnerUserId or TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.ServiceName,
            @event.EnvironmentName,
            @event.AffectedServiceCount
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.BlastRadiusHigh,
            Category = nameof(NotificationCategory.Change),
            Severity = nameof(NotificationSeverity.Warning),
            Title = $"High blast radius — {@event.ServiceName}",
            Message = $"Change for service {@event.ServiceName} in {@event.EnvironmentName} has a high blast radius affecting {@event.AffectedServiceCount} service(s). Review impact before proceeding.",
            SourceModule = "ChangeGovernance",
            SourceEntityType = "Change",
            SourceEntityId = @event.ChangeId.ToString(),
            ActionUrl = $"/changes/{@event.ChangeId}/blast-radius",
            RequiresAction = true,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.OwnerUserId.Value],
            PayloadJson = payload
        }, ct);
    }

    public async Task HandleAsync(PostChangeVerificationFailedIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing PostChangeVerificationFailed notification for change {ChangeId}, service {ServiceName}",
            @event.ChangeId, @event.ServiceName);

        if (@event.OwnerUserId is null || @event.TenantId is null)
        {
            logger.LogWarning("PostChangeVerificationFailed event missing OwnerUserId or TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.ServiceName,
            @event.EnvironmentName,
            @event.FailureReason
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.PostChangeVerificationFailed,
            Category = nameof(NotificationCategory.Change),
            Severity = nameof(NotificationSeverity.Critical),
            Title = $"Post-change verification failed — {@event.ServiceName}",
            Message = $"Post-change verification for service {@event.ServiceName} in {@event.EnvironmentName} failed. Reason: {@event.FailureReason}. Investigate immediately.",
            SourceModule = "ChangeGovernance",
            SourceEntityType = "Change",
            SourceEntityId = @event.ChangeId.ToString(),
            ActionUrl = $"/changes/{@event.ChangeId}/verification",
            RequiresAction = true,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.OwnerUserId.Value],
            PayloadJson = payload
        }, ct);
    }
}
