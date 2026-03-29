using NexTraceOne.Integrations.Application.LegacyTelemetry.Abstractions;
using NexTraceOne.Integrations.Application.LegacyTelemetry.Requests;
using NexTraceOne.Integrations.Domain.LegacyTelemetry;

namespace NexTraceOne.Integrations.Application.LegacyTelemetry.Parsers;

/// <summary>
/// Parser para eventos operacionais mainframe genéricos.
/// Normaliza MainframeEventRequest em NormalizedLegacyEvent canónico.
/// </summary>
public sealed class MainframeEventParser : ILegacyEventParser<MainframeEventRequest>
{
    public NormalizedLegacyEvent Parse(MainframeEventRequest request)
    {
        var severity = LegacySeverity.Normalize(request.Severity);
        var attributes = new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(request.Provider)) attributes["provider"] = request.Provider;
        if (!string.IsNullOrWhiteSpace(request.SourceType)) attributes["source_type"] = request.SourceType;
        if (!string.IsNullOrWhiteSpace(request.EventType)) attributes["event_type"] = request.EventType;
        if (!string.IsNullOrWhiteSpace(request.CorrelationId)) attributes["correlation_id"] = request.CorrelationId;

        if (request.Metadata is not null)
        {
            foreach (var kvp in request.Metadata)
                attributes.TryAdd(kvp.Key, kvp.Value);
        }

        var eventType = DetermineEventType(request.SourceType, request.EventType);

        return new NormalizedLegacyEvent(
            EventId: Guid.NewGuid().ToString("N"),
            EventType: eventType,
            SourceType: MapSourceType(request.SourceType),
            SystemName: request.SystemName,
            LparName: request.LparName,
            ServiceName: request.SystemName,
            AssetName: null,
            Severity: severity,
            Message: request.Message,
            Timestamp: request.EventTimestamp ?? DateTimeOffset.UtcNow,
            Attributes: attributes);
    }

    private static string DetermineEventType(string? sourceType, string? eventType)
    {
        if (!string.IsNullOrWhiteSpace(eventType))
            return eventType.ToLowerInvariant();

        return sourceType?.ToLowerInvariant() switch
        {
            "smf" => "mainframe_smf",
            "syslog" => "mainframe_syslog",
            "cics_stat" => "cics_statistics",
            "ims_stat" => "ims_statistics",
            "operational" => "mainframe_operational",
            _ => "mainframe_event"
        };
    }

    private static string MapSourceType(string? sourceType) =>
        sourceType?.ToLowerInvariant() switch
        {
            "cics_stat" => LegacyEventSourceType.Cics,
            "ims_stat" => LegacyEventSourceType.Ims,
            _ => LegacyEventSourceType.Mainframe
        };
}
