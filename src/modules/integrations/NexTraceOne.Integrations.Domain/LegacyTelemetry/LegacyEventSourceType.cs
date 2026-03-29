namespace NexTraceOne.Integrations.Domain.LegacyTelemetry;

/// <summary>
/// Tipos de fonte de telemetria legacy suportados.
/// </summary>
public static class LegacyEventSourceType
{
    public const string Batch = "batch";
    public const string Mq = "mq";
    public const string Cics = "cics";
    public const string Ims = "ims";
    public const string Mainframe = "mainframe";

    public static readonly string[] All = [Batch, Mq, Cics, Ims, Mainframe];
}
