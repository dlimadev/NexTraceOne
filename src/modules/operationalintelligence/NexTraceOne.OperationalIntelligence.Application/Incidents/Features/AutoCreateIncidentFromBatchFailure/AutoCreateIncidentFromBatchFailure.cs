using MediatR;
using Microsoft.Extensions.Logging;
using NexTraceOne.Integrations.Domain.LegacyTelemetry.Events;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

using CreateIncidentFeature = NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CreateIncident.CreateIncident;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.AutoCreateIncidentFromBatchFailure;

/// <summary>
/// Cria incidente automaticamente quando um evento batch legacy indica falha (abend/failed).
/// </summary>
public static class AutoCreateIncidentFromBatchFailure
{
    public sealed class Handler(
        ISender sender,
        ILegacyEventCorrelator correlator,
        ILogger<Handler> logger) : INotificationHandler<LegacyBatchEventIngestedEvent>
    {
        public async Task Handle(LegacyBatchEventIngestedEvent notification, CancellationToken cancellationToken)
        {
            var isFailed = string.Equals(notification.Status, "failed", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(notification.Status, "abended", StringComparison.OrdinalIgnoreCase) ||
                           (notification.ReturnCode?.StartsWith("ABEND", StringComparison.OrdinalIgnoreCase) ?? false);

            if (!isFailed) return;

            logger.LogInformation("Auto-creating incident for failed batch job {JobName} (RC={ReturnCode}, Status={Status})",
                notification.JobName, notification.ReturnCode, notification.Status);

            var correlation = !string.IsNullOrWhiteSpace(notification.JobName)
                ? await correlator.CorrelateByJobNameAsync(notification.JobName, notification.SystemName, cancellationToken)
                : null;

            var severity = string.Equals(notification.Status, "abended", StringComparison.OrdinalIgnoreCase) ||
                           (notification.ReturnCode?.StartsWith("ABEND", StringComparison.OrdinalIgnoreCase) ?? false)
                ? IncidentSeverity.Critical
                : IncidentSeverity.Major;

            var title = $"Batch job failure: {notification.JobName ?? "Unknown"} ({notification.ReturnCode ?? notification.Status})";
            var description = $"Automatic incident created from batch event.\n" +
                              $"Job: {notification.JobName}\n" +
                              $"Job ID: {notification.JobId}\n" +
                              $"Program: {notification.ProgramName}\n" +
                              $"Return Code: {notification.ReturnCode}\n" +
                              $"Status: {notification.Status}\n" +
                              $"System: {notification.SystemName}\n" +
                              $"LPAR: {notification.LparName}\n" +
                              $"Timestamp: {notification.Timestamp:O}";

            try
            {
                var command = new CreateIncidentFeature.Command(
                    Title: title,
                    Description: description,
                    IncidentType: IncidentType.BackgroundProcessingIssue,
                    Severity: severity,
                    ServiceId: correlation?.AssetId?.ToString() ?? notification.SystemName ?? "unknown",
                    ServiceDisplayName: correlation?.ServiceName ?? notification.JobName ?? "Legacy Batch Job",
                    OwnerTeam: "legacy-operations",
                    ImpactedDomain: "mainframe",
                    Environment: "production",
                    DetectedAtUtc: notification.Timestamp);

                var result = await sender.Send(command, cancellationToken);

                if (result.IsSuccess)
                {
                    logger.LogInformation("Auto-created incident {IncidentRef} for batch failure {JobName}",
                        result.Value.Reference, notification.JobName);
                }
                else
                {
                    logger.LogWarning("Failed to auto-create incident for batch failure {JobName}: {Error}",
                        notification.JobName, result.Error);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error auto-creating incident for batch failure {JobName}", notification.JobName);
            }
        }
    }
}
