using MediatR;
using Microsoft.Extensions.Logging;
using NexTraceOne.Integrations.Domain.LegacyTelemetry.Events;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CorrelateBatchEvent;

/// <summary>
/// Handler de correlação de eventos batch legacy com ativos do catálogo.
/// </summary>
public static class CorrelateBatchEvent
{
    public sealed class Handler(
        ILegacyEventCorrelator correlator,
        ILogger<Handler> logger) : INotificationHandler<LegacyBatchEventIngestedEvent>
    {
        public async Task Handle(LegacyBatchEventIngestedEvent notification, CancellationToken cancellationToken)
        {
            logger.LogInformation("Correlating batch event {EventId} for job {JobName}",
                notification.IngestionEventId, notification.JobName);

            if (string.IsNullOrWhiteSpace(notification.JobName)) return;

            var result = await correlator.CorrelateByJobNameAsync(
                notification.JobName, notification.SystemName, cancellationToken);

            if (result.IsCorrelated)
            {
                logger.LogInformation("Batch event {EventId} correlated with {AssetType} {AssetName} via {MatchMethod}",
                    notification.IngestionEventId, result.AssetType, result.AssetName, result.MatchMethod);
            }
            else
            {
                logger.LogDebug("Batch event {EventId} for job {JobName} did not correlate with any catalog asset",
                    notification.IngestionEventId, notification.JobName);
            }
        }
    }
}
