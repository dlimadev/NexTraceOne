namespace NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

/// <summary>
/// Serviço de retrieval de telemetria para grounding de IA.
/// Abstrai a pesquisa em dados de traces, logs e métricas para contextualizar respostas de IA.
/// Fundação para futura integração com OpenTelemetry Collector e backends de observabilidade.
/// </summary>
public interface ITelemetryRetrievalService
{
    /// <summary>Pesquisa dados de telemetria relevantes para grounding de IA.</summary>
    Task<TelemetrySearchResult> SearchAsync(
        TelemetrySearchRequest request,
        CancellationToken ct = default);
}

/// <summary>Request de pesquisa de telemetria.</summary>
public sealed record TelemetrySearchRequest(
    string Query,
    string? TraceId = null,
    string? SpanId = null,
    string? ServiceName = null,
    string? Severity = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int MaxResults = 50);

/// <summary>Resultado de pesquisa de telemetria.</summary>
public sealed record TelemetrySearchResult(
    bool Success,
    IReadOnlyList<TelemetrySearchHit> Hits,
    string? ErrorMessage = null);

/// <summary>Hit individual de pesquisa de telemetria.</summary>
public sealed record TelemetrySearchHit(
    string TraceId,
    string? SpanId,
    string ServiceName,
    string Message,
    string Severity,
    DateTimeOffset Timestamp,
    double? DurationMs = null);
