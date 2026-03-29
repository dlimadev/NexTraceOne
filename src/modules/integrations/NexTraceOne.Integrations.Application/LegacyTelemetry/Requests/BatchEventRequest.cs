namespace NexTraceOne.Integrations.Application.LegacyTelemetry.Requests;

/// <summary>
/// Payload de ingestão de evento de execução batch mainframe.
/// </summary>
public sealed record BatchEventRequest(
    string? Provider,
    string? CorrelationId,
    string? JobName,
    string? JobId,
    string? StepName,
    string? ProgramName,
    string? ReturnCode,
    string? Status,
    string? SystemName,
    string? LparName,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    long? DurationMs,
    string? ChainName,
    Dictionary<string, string>? Metadata);
