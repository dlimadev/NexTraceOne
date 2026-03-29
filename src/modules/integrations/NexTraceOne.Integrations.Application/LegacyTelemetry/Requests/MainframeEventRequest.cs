namespace NexTraceOne.Integrations.Application.LegacyTelemetry.Requests;

/// <summary>
/// Payload de ingestão de evento operacional mainframe genérico.
/// </summary>
public sealed record MainframeEventRequest(
    string? Provider,
    string? CorrelationId,
    string? SourceType,
    string? SystemName,
    string? LparName,
    string? EventType,
    string? Message,
    string? Severity,
    DateTimeOffset? EventTimestamp,
    Dictionary<string, string>? Metadata);
