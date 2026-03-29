namespace NexTraceOne.Integrations.Domain.LegacyTelemetry;

/// <summary>
/// Níveis de severidade para eventos legacy normalizados.
/// </summary>
public static class LegacySeverity
{
    public const string Info = "info";
    public const string Warning = "warning";
    public const string Error = "error";
    public const string Critical = "critical";

    public static string Normalize(string? severity) =>
        severity?.ToLowerInvariant() switch
        {
            "info" or "information" or "informational" => Info,
            "warn" or "warning" => Warning,
            "error" or "err" => Error,
            "critical" or "crit" or "fatal" => Critical,
            _ => Info
        };
}
