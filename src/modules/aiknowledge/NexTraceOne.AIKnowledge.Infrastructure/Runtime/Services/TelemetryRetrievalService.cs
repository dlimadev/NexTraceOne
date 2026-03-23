using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Observability.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Observability.Models;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

/// <summary>
/// Implementação real do serviço de retrieval de telemetria para grounding de IA.
/// Pesquisa dados de logs reais via IObservabilityProvider do módulo de observabilidade.
/// Retorna contexto operacional real ou vazio honesto quando não há dados disponíveis.
/// </summary>
public sealed class TelemetryRetrievalService : ITelemetryRetrievalService
{
    private readonly IObservabilityProvider _observabilityProvider;
    private readonly ILogger<TelemetryRetrievalService> _logger;

    public TelemetryRetrievalService(
        IObservabilityProvider observabilityProvider,
        ILogger<TelemetryRetrievalService> logger)
    {
        _observabilityProvider = observabilityProvider;
        _logger = logger;
    }

    public async Task<TelemetrySearchResult> SearchAsync(
        TelemetrySearchRequest request,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Telemetry retrieval requested for query '{Query}', service '{ServiceName}', max {MaxResults} results",
            request.Query, request.ServiceName, request.MaxResults);

        try
        {
            var filter = new LogQueryFilter
            {
                Environment = "Production",
                From = request.From ?? DateTimeOffset.UtcNow.AddHours(-1),
                Until = request.To ?? DateTimeOffset.UtcNow,
                ServiceName = request.ServiceName,
                Level = request.Severity,
                MessageContains = request.Query,
                TraceId = request.TraceId,
                Limit = request.MaxResults
            };

            var logs = await _observabilityProvider.QueryLogsAsync(filter, ct);

            var hits = logs
                .Select(log => new TelemetrySearchHit(
                    TraceId: log.TraceId ?? string.Empty,
                    SpanId: log.SpanId,
                    ServiceName: log.ServiceName,
                    Message: log.Message,
                    Severity: log.Level,
                    Timestamp: log.Timestamp))
                .ToList();

            _logger.LogDebug(
                "Telemetry retrieval found {HitCount} results for query '{Query}'",
                hits.Count, request.Query);

            return new TelemetrySearchResult(Success: true, Hits: hits);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Telemetry retrieval failed for query '{Query}'", request.Query);
            return new TelemetrySearchResult(
                Success: false,
                Hits: Array.Empty<TelemetrySearchHit>(),
                ErrorMessage: ex.Message);
        }
    }
}
