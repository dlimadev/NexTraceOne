using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

/// <summary>
/// Implementação stub do serviço de retrieval de telemetria.
/// Fundação para futura integração com OpenTelemetry Collector e backends de observabilidade.
/// Retorna resultados vazios até integração com fontes de traces, logs e métricas reais.
/// </summary>
public sealed class TelemetryRetrievalService : ITelemetryRetrievalService
{
    private readonly ILogger<TelemetryRetrievalService> _logger;

    public TelemetryRetrievalService(ILogger<TelemetryRetrievalService> logger)
    {
        _logger = logger;
    }

    public Task<TelemetrySearchResult> SearchAsync(
        TelemetrySearchRequest request,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Telemetry retrieval requested for query '{Query}', service '{ServiceName}', max {MaxResults} results — full implementation pending (OpenTelemetry Collector integration required)",
            request.Query, request.ServiceName, request.MaxResults);

        var result = new TelemetrySearchResult(
            Success: true,
            Hits: Array.Empty<TelemetrySearchHit>(),
            ErrorMessage: null);

        return Task.FromResult(result);
    }
}
