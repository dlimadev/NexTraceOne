using MediatR;
using Microsoft.Extensions.Logging;
using NexTraceOne.Integrations.Domain.LegacyTelemetry.Events;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CorrelateMqEvent;

/// <summary>
/// Handler de correlação de eventos MQ legacy com ativos do catálogo.
/// </summary>
public static class CorrelateMqEvent
{
    public sealed class Handler(
        ILegacyEventCorrelator correlator,
        ILogger<Handler> logger) : INotificationHandler<LegacyMqEventIngestedEvent>
    {
        public async Task Handle(LegacyMqEventIngestedEvent notification, CancellationToken cancellationToken)
        {
            logger.LogInformation("Correlating MQ event {EventId} for queue {QueueName}",
                notification.IngestionEventId, notification.QueueName);

            var result = await correlator.CorrelateByQueueAsync(
                notification.QueueManagerName, notification.QueueName, cancellationToken);

            if (result.IsCorrelated)
            {
                logger.LogInformation("MQ event {EventId} correlated with {AssetType} {AssetName} via {MatchMethod}",
                    notification.IngestionEventId, result.AssetType, result.AssetName, result.MatchMethod);
            }
        }
    }
}
