using System.Text.Json;
using NexTraceOne.Integrations.Domain.LegacyTelemetry;

namespace NexTraceOne.Integrations.Application.LegacyTelemetry.Parsers;

/// <summary>
/// Parser para registos SMF em formato JSON pré-normalizado.
/// Não processa formato binário raw — assume que ferramentas IBM (Z CDP, OMEGAMON) 
/// já exportaram em JSON.
/// </summary>
public sealed class SmfRecordParser
{
    public NormalizedLegacyEvent Parse(string jsonInput)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonInput);

        var doc = JsonDocument.Parse(jsonInput);
        var root = doc.RootElement;

        var recordType = GetStringProperty(root, "record_type") ?? GetStringProperty(root, "smf_type") ?? "unknown";
        var systemName = GetStringProperty(root, "system_name") ?? GetStringProperty(root, "system");
        var lparName = GetStringProperty(root, "lpar_name") ?? GetStringProperty(root, "lpar");
        var timestamp = GetTimestamp(root);
        var severity = GetStringProperty(root, "severity") ?? "info";
        var message = GetStringProperty(root, "message") ?? GetStringProperty(root, "description");

        var attributes = new Dictionary<string, string>
        {
            ["provider"] = "SMF",
            ["record_type"] = recordType
        };

        ExtractFlatAttributes(root, attributes, ["record_type", "smf_type", "system_name", "system",
            "lpar_name", "lpar", "timestamp", "severity", "message", "description"]);

        return new NormalizedLegacyEvent(
            EventId: Guid.NewGuid().ToString("N"),
            EventType: $"smf_{recordType}",
            SourceType: LegacyEventSourceType.Mainframe,
            SystemName: systemName,
            LparName: lparName,
            ServiceName: systemName,
            AssetName: null,
            Severity: LegacySeverity.Normalize(severity),
            Message: message,
            Timestamp: timestamp,
            Attributes: attributes);
    }

    private static string? GetStringProperty(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;

    private static DateTimeOffset GetTimestamp(JsonElement root)
    {
        if (root.TryGetProperty("timestamp", out var ts))
        {
            if (ts.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(ts.GetString(), out var parsed))
                return parsed;
        }
        return DateTimeOffset.UtcNow;
    }

    private static void ExtractFlatAttributes(JsonElement root, Dictionary<string, string> attributes, string[] excludeKeys)
    {
        var exclude = new HashSet<string>(excludeKeys, StringComparer.OrdinalIgnoreCase);
        foreach (var prop in root.EnumerateObject())
        {
            if (exclude.Contains(prop.Name)) continue;
            if (prop.Value.ValueKind is JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
                attributes.TryAdd(prop.Name, prop.Value.ToString());
        }
    }
}
