using System.Text.Json;
using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.Governance.Contracts;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Infrastructure.EventHandlers;

/// <summary>
/// Handler para eventos de compliance e governança do módulo Governance.
/// Gera notificações internas para falhas de compliance, violações de política e evidências expirando.
/// Fase 5: adicionados PolicyViolated, EvidenceExpiring e BudgetThresholdReached.
/// </summary>
internal sealed class ComplianceNotificationHandler(
    INotificationModule notificationModule,
    ILogger<ComplianceNotificationHandler> logger)
    : IIntegrationEventHandler<IntegrationEvents.ComplianceCheckFailedIntegrationEvent>,
      IIntegrationEventHandler<IntegrationEvents.PolicyViolatedIntegrationEvent>,
      IIntegrationEventHandler<IntegrationEvents.EvidenceExpiringIntegrationEvent>,
      IIntegrationEventHandler<IntegrationEvents.BudgetThresholdReachedIntegrationEvent>
{
    public async Task HandleAsync(
        IntegrationEvents.ComplianceCheckFailedIntegrationEvent @event,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing ComplianceCheckFailed notification for report {ReportId}, service {ServiceName}",
            @event.ReportId, @event.ServiceName);

        if (@event.OwnerUserId is null || @event.TenantId is null)
        {
            logger.LogWarning("ComplianceCheckFailed event missing OwnerUserId or TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.ServiceName,
            @event.GapCount,
            @event.ReportId
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.ComplianceCheckFailed,
            Category = nameof(NotificationCategory.Compliance),
            Severity = nameof(NotificationSeverity.Warning),
            Title = $"Compliance check failed — {@event.ServiceName}",
            Message = $"{@event.GapCount} compliance gap(s) detected for {@event.ServiceName}. Review and remediate.",
            SourceModule = "Governance",
            SourceEntityType = "ComplianceReport",
            SourceEntityId = @event.ReportId,
            ActionUrl = $"/governance/compliance/{@event.ReportId}",
            RequiresAction = true,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.OwnerUserId.Value],
            PayloadJson = payload
        }, ct);
    }

    public async Task HandleAsync(
        IntegrationEvents.PolicyViolatedIntegrationEvent @event,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing PolicyViolated notification for policy {PolicyName}, service {ServiceName}",
            @event.PolicyName, @event.ServiceName);

        if (@event.OwnerUserId is null || @event.TenantId is null)
        {
            logger.LogWarning("PolicyViolated event missing OwnerUserId or TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.PolicyName,
            @event.ServiceName,
            @event.ViolationDescription
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.PolicyViolated,
            Category = nameof(NotificationCategory.Compliance),
            Severity = nameof(NotificationSeverity.Warning),
            Title = $"Policy violated — {@event.PolicyName}",
            Message = $"Policy {@event.PolicyName} has been violated for service {@event.ServiceName}: {@event.ViolationDescription}. Remediate promptly.",
            SourceModule = "Governance",
            SourceEntityType = "Policy",
            SourceEntityId = @event.PolicyName,
            ActionUrl = $"/governance/policies/{Uri.EscapeDataString(@event.PolicyName)}",
            RequiresAction = true,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.OwnerUserId.Value],
            PayloadJson = payload
        }, ct);
    }

    public async Task HandleAsync(
        IntegrationEvents.EvidenceExpiringIntegrationEvent @event,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing EvidenceExpiring notification for evidence {EvidenceId}, service {ServiceName}",
            @event.EvidenceId, @event.ServiceName);

        if (@event.OwnerUserId is null || @event.TenantId is null)
        {
            logger.LogWarning("EvidenceExpiring event missing OwnerUserId or TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.EvidenceName,
            @event.ServiceName,
            ExpiresAt = @event.ExpiresAt.ToString("O")
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.EvidenceExpiring,
            Category = nameof(NotificationCategory.Compliance),
            Severity = nameof(NotificationSeverity.ActionRequired),
            Title = $"Evidence expiring — {@event.EvidenceName}",
            Message = $"Compliance evidence {@event.EvidenceName} for service {@event.ServiceName} is expiring at {@event.ExpiresAt:d}. Renew before deadline.",
            SourceModule = "Governance",
            SourceEntityType = "Evidence",
            SourceEntityId = @event.EvidenceId.ToString(),
            ActionUrl = $"/governance/evidence/{@event.EvidenceId}",
            RequiresAction = true,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.OwnerUserId.Value],
            PayloadJson = payload
        }, ct);
    }

    public async Task HandleAsync(
        IntegrationEvents.BudgetThresholdReachedIntegrationEvent @event,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing BudgetThresholdReached notification for service {ServiceName}, threshold {ThresholdPercent}%",
            @event.ServiceName, @event.ThresholdPercent);

        if (@event.OwnerUserId is null || @event.TenantId is null)
        {
            logger.LogWarning("BudgetThresholdReached event missing OwnerUserId or TenantId. Skipping.");
            return;
        }

        var severity = @event.ThresholdPercent >= 100
            ? nameof(NotificationSeverity.Critical)
            : @event.ThresholdPercent >= 90
                ? nameof(NotificationSeverity.Warning)
                : nameof(NotificationSeverity.ActionRequired);

        var payload = JsonSerializer.Serialize(new
        {
            @event.ServiceName,
            @event.ThresholdPercent,
            CurrentSpend = @event.CurrentSpend.ToString("F2"),
            BudgetLimit = @event.BudgetLimit.ToString("F2")
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.BudgetThresholdReached,
            Category = nameof(NotificationCategory.FinOps),
            Severity = severity,
            Title = $"Budget at {@event.ThresholdPercent}% — {@event.ServiceName}",
            Message = $"Service {@event.ServiceName} has reached {@event.ThresholdPercent}% of budget (${@event.CurrentSpend:N2} of ${@event.BudgetLimit:N2}). Review spending.",
            SourceModule = "Governance",
            SourceEntityType = "Budget",
            SourceEntityId = @event.ServiceName,
            ActionUrl = $"/finops/budgets?service={Uri.EscapeDataString(@event.ServiceName)}",
            RequiresAction = @event.ThresholdPercent >= 90,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.OwnerUserId.Value],
            PayloadJson = payload
        }, ct);
    }
}
