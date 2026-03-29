namespace NexTraceOne.Integrations.Application.LegacyTelemetry.Requests;

/// <summary>
/// Payload de ingestão de evento IBM MQ.
/// </summary>
public sealed record MqEventRequest(
    string? Provider,
    string? CorrelationId,
    string? QueueManagerName,
    string? QueueName,
    string? ChannelName,
    string? EventType,
    int? QueueDepth,
    int? MaxDepth,
    long? EnqueueCount,
    long? DequeueCount,
    string? ChannelStatus,
    DateTimeOffset? EventTimestamp,
    Dictionary<string, string>? Metadata);
