using System.Text.Json;
using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Infrastructure.EventBus.Abstractions;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.OperationalIntelligence.Contracts.IntegrationEvents;

namespace NexTraceOne.Notifications.Infrastructure.EventHandlers;

/// <summary>
/// Handler para eventos de custo e orçamento do módulo Operational Intelligence.
/// Gera notificações internas quando anomalias de custo excedem o orçamento.
/// </summary>
internal sealed class BudgetNotificationHandler(
    INotificationModule notificationModule,
    ILogger<BudgetNotificationHandler> logger)
    : IIntegrationEventHandler<BudgetExceededIntegrationEvent>
{
    public async Task HandleAsync(BudgetExceededIntegrationEvent @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing BudgetExceeded notification for anomaly {AnomalyId}, service {ServiceName}",
            @event.AnomalyId, @event.ServiceName);

        if (@event.OwnerUserId is null || @event.TenantId is null)
        {
            logger.LogWarning("BudgetExceeded event missing OwnerUserId or TenantId. Skipping.");
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            @event.ServiceName,
            ExpectedCost = @event.ExpectedCost.ToString("F2"),
            ActualCost = @event.ActualCost.ToString("F2")
        });

        await notificationModule.SubmitAsync(new NotificationRequest
        {
            EventType = NotificationType.BudgetExceeded,
            Category = nameof(NotificationCategory.FinOps),
            Severity = nameof(NotificationSeverity.Warning),
            Title = $"Budget exceeded — {@event.ServiceName}",
            Message = $"Cost anomaly detected for {@event.ServiceName}: expected {FormatCost(@event.ExpectedCost)}, actual {FormatCost(@event.ActualCost)}. Review immediately.",
            SourceModule = "OperationalIntelligence",
            SourceEntityType = "CostAnomaly",
            SourceEntityId = @event.AnomalyId.ToString(),
            ActionUrl = $"/finops/anomalies/{@event.AnomalyId}",
            RequiresAction = true,
            TenantId = @event.TenantId,
            RecipientUserIds = [@event.OwnerUserId.Value],
            PayloadJson = payload
        }, ct);
    }

    private static string FormatCost(decimal cost) => $"${cost:N2}";
}
