using NexTraceOne.Governance.Application.Features.IngestOtelMetrics;

namespace NexTraceOne.Governance.Application.Abstractions;

/// <summary>
/// Repositório para persistência e consulta de métricas OpenTelemetry.
/// A implementação concreta decide o storage backend (PostgreSQL, ClickHouse, Elasticsearch).
/// No MVP usa PostgreSQL — a abstração permite migração sem mudança no domínio.
/// </summary>
public interface IOtelMetricRepository
{
    /// <summary>
    /// Persiste um batch de datapoints OTLP de forma eficiente (bulk insert).
    /// Retorna o número de datapoints efectivamente persistidos.
    /// Datapoints com erros de validação são descartados sem falhar o batch inteiro.
    /// </summary>
    Task<int> BatchInsertAsync(
        IReadOnlyList<OtelMetricDataPoint> dataPoints,
        CancellationToken cancellationToken);

    /// <summary>
    /// Consulta métricas para um serviço e janela de tempo.
    /// Usado por Change Intelligence para correlacionar degradação com deploys.
    /// </summary>
    Task<IReadOnlyList<OtelMetricDataPoint>> QueryAsync(
        string serviceName,
        string metricName,
        DateTimeOffset from,
        DateTimeOffset to,
        string? environment = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna a lista de nomes de serviços distintos com métricas ingeridas.
    /// Usado para enumerar serviços observados no Change Intelligence.
    /// </summary>
    Task<IReadOnlyList<string>> GetDistinctServiceNamesAsync(
        CancellationToken cancellationToken = default);
}
