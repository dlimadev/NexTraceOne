using NexTraceOne.Integrations.Application.LegacyTelemetry.Abstractions;
using NexTraceOne.Integrations.Application.LegacyTelemetry.Requests;
using NexTraceOne.Integrations.Domain.LegacyTelemetry;

namespace NexTraceOne.Integrations.Application.LegacyTelemetry.Parsers;

/// <summary>
/// Parser para eventos operacionais IBM MQ.
/// Normaliza MqEventRequest em NormalizedLegacyEvent canónico.
/// </summary>
public sealed class MqEventParser : ILegacyEventParser<MqEventRequest>
{
    public NormalizedLegacyEvent Parse(MqEventRequest request)
    {
        var severity = DetermineMqSeverity(request.EventType, request.QueueDepth, request.MaxDepth, request.ChannelStatus);
        var attributes = new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(request.Provider)) attributes["provider"] = request.Provider;
        if (!string.IsNullOrWhiteSpace(request.QueueManagerName)) attributes["queue_manager"] = request.QueueManagerName;
        if (!string.IsNullOrWhiteSpace(request.QueueName)) attributes["queue_name"] = request.QueueName;
        if (!string.IsNullOrWhiteSpace(request.ChannelName)) attributes["channel_name"] = request.ChannelName;
        if (!string.IsNullOrWhiteSpace(request.EventType)) attributes["event_type"] = request.EventType;
        if (request.QueueDepth.HasValue) attributes["queue_depth"] = request.QueueDepth.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (request.MaxDepth.HasValue) attributes["max_depth"] = request.MaxDepth.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (request.EnqueueCount.HasValue) attributes["enqueue_count"] = request.EnqueueCount.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (request.DequeueCount.HasValue) attributes["dequeue_count"] = request.DequeueCount.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(request.ChannelStatus)) attributes["channel_status"] = request.ChannelStatus;
        if (!string.IsNullOrWhiteSpace(request.CorrelationId)) attributes["correlation_id"] = request.CorrelationId;

        if (request.Metadata is not null)
        {
            foreach (var kvp in request.Metadata)
                attributes.TryAdd(kvp.Key, kvp.Value);
        }

        var eventType = DetermineMqEventType(request.EventType);
        var message = BuildMqMessage(request);

        return new NormalizedLegacyEvent(
            EventId: Guid.NewGuid().ToString("N"),
            EventType: eventType,
            SourceType: LegacyEventSourceType.Mq,
            SystemName: request.QueueManagerName,
            LparName: null,
            ServiceName: request.QueueManagerName,
            AssetName: request.QueueName ?? request.ChannelName ?? request.QueueManagerName,
            Severity: severity,
            Message: message,
            Timestamp: request.EventTimestamp ?? DateTimeOffset.UtcNow,
            Attributes: attributes);
    }

    private static string DetermineMqSeverity(string? eventType, int? queueDepth, int? maxDepth, string? channelStatus)
    {
        if (string.Equals(eventType, "dlq_message", StringComparison.OrdinalIgnoreCase))
            return LegacySeverity.Error;

        if (string.Equals(channelStatus, "stopped", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(channelStatus, "retrying", StringComparison.OrdinalIgnoreCase))
            return LegacySeverity.Warning;

        if (queueDepth.HasValue && maxDepth.HasValue && maxDepth.Value > 0)
        {
            var ratio = (double)queueDepth.Value / maxDepth.Value;
            if (ratio >= 0.9) return LegacySeverity.Critical;
            if (ratio >= 0.7) return LegacySeverity.Warning;
        }

        if (string.Equals(eventType, "depth_threshold", StringComparison.OrdinalIgnoreCase))
            return LegacySeverity.Warning;

        return LegacySeverity.Info;
    }

    private static string DetermineMqEventType(string? eventType) =>
        eventType?.ToLowerInvariant() switch
        {
            "depth_threshold" => "mq_depth_threshold",
            "dlq_message" => "mq_dead_letter",
            "channel_status" => "mq_channel_status",
            "statistics" => "mq_statistics",
            _ => "mq_operational"
        };

    private static string BuildMqMessage(MqEventRequest request)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.QueueManagerName)) parts.Add($"QM={request.QueueManagerName}");
        if (!string.IsNullOrWhiteSpace(request.EventType)) parts.Add($"Event={request.EventType}");
        if (!string.IsNullOrWhiteSpace(request.QueueName)) parts.Add($"Queue={request.QueueName}");
        if (request.QueueDepth.HasValue) parts.Add($"Depth={request.QueueDepth}");
        return parts.Count > 0 ? string.Join(", ", parts) : "MQ event";
    }
}
