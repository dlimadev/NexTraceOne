using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.IngestOtelMetrics;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação ClickHouse do IOtelMetricRepository.
/// Tabela alvo: nextraceone_obs.otel_metrics (JSONEachRow via HTTP INSERT).
/// Usa a mesma convenção de endpoint/config do ClickHouseLegacyEventWriter.
/// Activado quando Telemetry:ObservabilityProvider:Provider = "ClickHouse".
/// </summary>
internal sealed class ClickHouseOtelMetricRepository(
    HttpClient httpClient,
    IOptions<ClickHouseOtelMetricOptions> options,
    ILogger<ClickHouseOtelMetricRepository> logger) : IOtelMetricRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<int> BatchInsertAsync(
        IReadOnlyList<OtelMetricDataPoint> dataPoints,
        CancellationToken cancellationToken)
    {
        if (dataPoints.Count == 0)
            return 0;

        try
        {
            var rows = dataPoints.Select(dp => new ClickHouseOtelRow(
                Timestamp: dp.Timestamp,
                MetricName: dp.MetricName,
                MetricType: dp.MetricType.ToString(),
                Value: dp.Value,
                ServiceName: dp.ServiceName ?? string.Empty,
                ServiceVersion: dp.ServiceVersion,
                Environment: dp.Environment,
                ResourceAttributes: dp.ResourceAttributes.ToDictionary(k => k.Key, v => v.Value),
                MetricAttributes: dp.MetricAttributes.ToDictionary(k => k.Key, v => v.Value),
                IngestedAt: DateTimeOffset.UtcNow
            )).ToList();

            var query = "INSERT INTO nextraceone_obs.otel_metrics FORMAT JSONEachRow";
            var url = $"{options.Value.Endpoint}/?query={Uri.EscapeDataString(query)}";

            var jsonLines = string.Join("\n", rows.Select(r => JsonSerializer.Serialize(r, JsonOptions)));
            var content = new StringContent(jsonLines, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(url, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            logger.LogInformation("Inserted {Count} OTEL metrics into ClickHouse", rows.Count);
            return rows.Count;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to insert {Count} OTEL metrics into ClickHouse", dataPoints.Count);

            if (!options.Value.SuppressWriteErrors)
                throw;

            return 0;
        }
    }

    public async Task<IReadOnlyList<OtelMetricDataPoint>> QueryAsync(
        string serviceName,
        string metricName,
        DateTimeOffset from,
        DateTimeOffset to,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var envClause = string.IsNullOrWhiteSpace(environment)
                ? string.Empty
                : $" AND environment = '{environment.Replace("'", "\\'")}'";

            var sql = $"""
                SELECT metric_name, metric_type, value, service_name, service_version,
                       environment, timestamp, resource_attributes, metric_attributes
                FROM nextraceone_obs.otel_metrics
                WHERE service_name = '{serviceName.Replace("'", "\\'")}'
                  AND metric_name  = '{metricName.Replace("'", "\\'")}'
                  AND timestamp >= '{from.UtcDateTime:yyyy-MM-dd HH:mm:ss}'
                  AND timestamp <= '{to.UtcDateTime:yyyy-MM-dd HH:mm:ss}'{envClause}
                ORDER BY timestamp ASC
                FORMAT JSONEachRow
                """;

            var url = $"{options.Value.Endpoint}/?query={Uri.EscapeDataString(sql)}";
            var responseText = await httpClient.GetStringAsync(url, cancellationToken);

            var results = new List<OtelMetricDataPoint>();
            foreach (var line in responseText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var row = JsonSerializer.Deserialize<ClickHouseOtelRow>(line, JsonOptions);
                if (row is null) continue;

                results.Add(new OtelMetricDataPoint
                {
                    MetricName = row.MetricName,
                    MetricType = Enum.TryParse<OtelMetricType>(row.MetricType, out var t) ? t : OtelMetricType.Gauge,
                    Value = row.Value,
                    Timestamp = row.Timestamp,
                    ResourceAttributes = row.ResourceAttributes ?? new Dictionary<string, string>(),
                    MetricAttributes = row.MetricAttributes ?? new Dictionary<string, string>(),
                });
            }

            return results;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to query OTEL metrics from ClickHouse for service {ServiceName}", serviceName);
            return [];
        }
    }

    public async Task<IReadOnlyList<string>> GetDistinctServiceNamesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = "SELECT DISTINCT service_name FROM nextraceone_obs.otel_metrics ORDER BY service_name FORMAT JSONEachRow";
            var url = $"{options.Value.Endpoint}/?query={Uri.EscapeDataString(sql)}";
            var responseText = await httpClient.GetStringAsync(url, cancellationToken);

            var names = new List<string>();
            foreach (var line in responseText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                using var doc = JsonDocument.Parse(line);
                if (doc.RootElement.TryGetProperty("service_name", out var prop))
                    names.Add(prop.GetString() ?? string.Empty);
            }

            return names;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to query distinct service names from ClickHouse");
            return [];
        }
    }

    private sealed record ClickHouseOtelRow(
        DateTimeOffset Timestamp,
        string MetricName,
        string MetricType,
        string Value,
        string ServiceName,
        string? ServiceVersion,
        string? Environment,
        Dictionary<string, string>? ResourceAttributes,
        Dictionary<string, string>? MetricAttributes,
        DateTimeOffset IngestedAt);
}

/// <summary>
/// Configuração do repositório ClickHouse para métricas OTEL.
/// </summary>
public sealed class ClickHouseOtelMetricOptions
{
    public const string SectionName = "ClickHouse:LegacyTelemetry";

    public string Endpoint { get; set; } = "http://localhost:8123";
    public bool SuppressWriteErrors { get; set; } = true;
}
