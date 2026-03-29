using NexTraceOne.BuildingBlocks.Core.Events;

namespace NexTraceOne.Integrations.Domain.LegacyTelemetry.Events;

/// <summary>
/// Evento publicado após ingestão bem-sucedida de evento MQ legacy.
/// </summary>
public sealed record LegacyMqEventIngestedEvent(
    string IngestionEventId,
    string? QueueManagerName,
    string? QueueName,
    string? ChannelName,
    string? EventType,
    int? QueueDepth,
    int? MaxDepth,
    string? ChannelStatus,
    string Severity,
    string? Message,
    DateTimeOffset Timestamp) : DomainEventBase;
