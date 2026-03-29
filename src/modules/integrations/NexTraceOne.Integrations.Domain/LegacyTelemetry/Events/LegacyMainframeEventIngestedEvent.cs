using NexTraceOne.BuildingBlocks.Core.Events;

namespace NexTraceOne.Integrations.Domain.LegacyTelemetry.Events;

/// <summary>
/// Evento publicado após ingestão bem-sucedida de evento mainframe legacy genérico.
/// </summary>
public sealed record LegacyMainframeEventIngestedEvent(
    string IngestionEventId,
    string? SourceType,
    string? SystemName,
    string? LparName,
    string? EventType,
    string Severity,
    string? Message,
    DateTimeOffset Timestamp) : DomainEventBase;
