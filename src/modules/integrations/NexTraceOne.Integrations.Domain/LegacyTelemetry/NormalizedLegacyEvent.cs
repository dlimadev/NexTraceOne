namespace NexTraceOne.Integrations.Domain.LegacyTelemetry;

/// <summary>
/// Modelo canónico para eventos de telemetria legacy normalizados.
/// Representa o formato semântico unificado para batch, MQ, CICS, IMS e mainframe events.
/// </summary>
public sealed record NormalizedLegacyEvent(
    string EventId,
    string EventType,
    string SourceType,
    string? SystemName,
    string? LparName,
    string? ServiceName,
    string? AssetName,
    string Severity,
    string? Message,
    DateTimeOffset Timestamp,
    Dictionary<string, string> Attributes);
