namespace NexTraceOne.Integrations.Domain.LegacyTelemetry;

/// <summary>
/// Resultado de correlação de evento legacy com ativos do catálogo.
/// </summary>
public sealed record CorrelationResult(
    bool IsCorrelated,
    string? AssetType,
    string? AssetName,
    Guid? AssetId,
    string? ServiceName,
    string MatchMethod,
    string? Details);
