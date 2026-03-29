using MediatR;
using Microsoft.Extensions.Logging;
using NexTraceOne.Integrations.Domain.LegacyTelemetry.Events;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CorrelateMainframeEvent;

/// <summary>
/// Handler de correlação de eventos mainframe legacy com ativos do catálogo.
/// </summary>
public static class CorrelateMainframeEvent
{
    public sealed class Handler(
        ILegacyEventCorrelator correlator,
        ILogger<Handler> logger) : INotificationHandler<LegacyMainframeEventIngestedEvent>
    {
        public async Task Handle(LegacyMainframeEventIngestedEvent notification, CancellationToken cancellationToken)
        {
            logger.LogInformation("Correlating mainframe event {EventId} for system {SystemName}",
                notification.IngestionEventId, notification.SystemName);

            if (string.IsNullOrWhiteSpace(notification.SystemName)) return;

            var result = await correlator.CorrelateBySystemNameAsync(
                notification.SystemName, cancellationToken);

            if (result.IsCorrelated)
            {
                logger.LogInformation("Mainframe event {EventId} correlated with {AssetType} {AssetName} via {MatchMethod}",
                    notification.IngestionEventId, result.AssetType, result.AssetName, result.MatchMethod);
            }
        }
    }
}
