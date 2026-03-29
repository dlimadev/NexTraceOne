using System.Text.RegularExpressions;
using NexTraceOne.Integrations.Domain.LegacyTelemetry;

namespace NexTraceOne.Integrations.Application.LegacyTelemetry.Parsers;

/// <summary>
/// Parser básico para linhas de SYSLOG z/OS.
/// Formato esperado: TIMESTAMP SYSTEM MESSAGE
/// </summary>
public sealed partial class SyslogParser
{
    [GeneratedRegex(@"^(?<timestamp>\S+)\s+(?<system>\S+)\s+(?<message>.+)$", RegexOptions.Compiled)]
    private static partial Regex SyslogLineRegex();

    public NormalizedLegacyEvent Parse(string syslogLine)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(syslogLine);

        var match = SyslogLineRegex().Match(syslogLine.Trim());
        string? systemName = null;
        string? message = null;
        var timestamp = DateTimeOffset.UtcNow;

        if (match.Success)
        {
            systemName = match.Groups["system"].Value;
            message = match.Groups["message"].Value;
            if (DateTimeOffset.TryParse(match.Groups["timestamp"].Value, out var parsed))
                timestamp = parsed;
        }
        else
        {
            message = syslogLine.Trim();
        }

        var severity = InferSyslogSeverity(message);

        var attributes = new Dictionary<string, string>
        {
            ["provider"] = "SYSLOG",
            ["raw_line"] = syslogLine.Length > 500 ? syslogLine[..500] : syslogLine
        };

        return new NormalizedLegacyEvent(
            EventId: Guid.NewGuid().ToString("N"),
            EventType: "mainframe_syslog",
            SourceType: LegacyEventSourceType.Mainframe,
            SystemName: systemName,
            LparName: null,
            ServiceName: systemName,
            AssetName: null,
            Severity: severity,
            Message: message,
            Timestamp: timestamp,
            Attributes: attributes);
    }

    private static string InferSyslogSeverity(string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return LegacySeverity.Info;

        var upper = message.ToUpperInvariant();
        if (upper.Contains("ABEND") || upper.Contains("FATAL") || upper.Contains("S0C"))
            return LegacySeverity.Critical;
        if (upper.Contains("ERROR") || upper.Contains("FAIL"))
            return LegacySeverity.Error;
        if (upper.Contains("WARN") || upper.Contains("IEF"))
            return LegacySeverity.Warning;

        return LegacySeverity.Info;
    }
}
