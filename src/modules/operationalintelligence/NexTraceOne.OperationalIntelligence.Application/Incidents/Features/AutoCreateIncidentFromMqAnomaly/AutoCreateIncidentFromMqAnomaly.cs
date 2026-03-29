using MediatR;
using Microsoft.Extensions.Logging;
using NexTraceOne.Integrations.Domain.LegacyTelemetry.Events;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

using CreateIncidentFeature = NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CreateIncident.CreateIncident;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.AutoCreateIncidentFromMqAnomaly;

/// <summary>
/// Cria incidente automaticamente quando um evento MQ legacy indica anomalia crítica
/// (DLQ, profundidade de fila ≥ 90%, ou severidade crítica).
/// </summary>
public static class AutoCreateIncidentFromMqAnomaly
{
    public sealed class Handler(
        ISender sender,
        ILegacyEventCorrelator correlator,
        ILogger<Handler> logger) : INotificationHandler<LegacyMqEventIngestedEvent>
    {
        public async Task Handle(LegacyMqEventIngestedEvent notification, CancellationToken cancellationToken)
        {
            var isDlq = string.Equals(notification.EventType, "dlq_message", StringComparison.OrdinalIgnoreCase);
            var isDepthCritical = notification.QueueDepth.HasValue && notification.MaxDepth.HasValue &&
                                  notification.MaxDepth.Value > 0 &&
                                  (double)notification.QueueDepth.Value / notification.MaxDepth.Value >= 0.9;
            var isCriticalSeverity = string.Equals(notification.Severity, "critical", StringComparison.OrdinalIgnoreCase);

            if (!isDlq && !isDepthCritical && !isCriticalSeverity) return;

            logger.LogInformation("Auto-creating incident for MQ anomaly: QM={QueueManager}, Queue={Queue}, Type={EventType}",
                notification.QueueManagerName, notification.QueueName, notification.EventType);

            var correlation = await correlator.CorrelateByQueueAsync(
                notification.QueueManagerName, notification.QueueName, cancellationToken);

            var severity = isDlq ? IncidentSeverity.Major :
                           isDepthCritical ? IncidentSeverity.Critical :
                           IncidentSeverity.Major;

            var anomalyType = isDlq ? "Dead Letter Queue message" :
                              isDepthCritical ? $"Queue depth threshold ({notification.QueueDepth}/{notification.MaxDepth})" :
                              "Critical MQ event";

            var title = $"MQ anomaly: {anomalyType} on {notification.QueueName ?? notification.QueueManagerName ?? "Unknown"}";
            var description = $"Automatic incident created from MQ event.\n" +
                              $"Queue Manager: {notification.QueueManagerName}\n" +
                              $"Queue: {notification.QueueName}\n" +
                              $"Channel: {notification.ChannelName}\n" +
                              $"Event Type: {notification.EventType}\n" +
                              $"Queue Depth: {notification.QueueDepth}/{notification.MaxDepth}\n" +
                              $"Channel Status: {notification.ChannelStatus}\n" +
                              $"Severity: {notification.Severity}\n" +
                              $"Timestamp: {notification.Timestamp:O}";

            try
            {
                var command = new CreateIncidentFeature.Command(
                    Title: title,
                    Description: description,
                    IncidentType: IncidentType.MessagingIssue,
                    Severity: severity,
                    ServiceId: correlation?.AssetId?.ToString() ?? notification.QueueManagerName ?? "unknown",
                    ServiceDisplayName: correlation?.ServiceName ?? notification.QueueManagerName ?? "Legacy MQ",
                    OwnerTeam: "legacy-operations",
                    ImpactedDomain: "mainframe",
                    Environment: "production",
                    DetectedAtUtc: notification.Timestamp);

                var result = await sender.Send(command, cancellationToken);

                if (result.IsSuccess)
                {
                    logger.LogInformation("Auto-created incident {IncidentRef} for MQ anomaly on {Queue}",
                        result.Value.Reference, notification.QueueName);
                }
                else
                {
                    logger.LogWarning("Failed to auto-create incident for MQ anomaly: {Error}", result.Error);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error auto-creating incident for MQ anomaly on {Queue}", notification.QueueName);
            }
        }
    }
}
