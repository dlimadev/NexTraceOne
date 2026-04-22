using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexTraceOne.BuildingBlocks.Observability.Analytics.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Analytics.Configuration;

namespace NexTraceOne.BuildingBlocks.Observability.Analytics.Readers;

/// <summary>
/// Implementação do IClickHouseAnalyticsReader via HTTP interface do ClickHouse (porta 8123).
/// Executa queries SELECT em formato JSONEachRow contra as tabelas obs_traces, obs_metrics e obs_logs.
///
/// Configuração: Analytics:ConnectionString deve apontar para o endpoint HTTP do ClickHouse.
/// Exemplo: http://clickhouse:8123/?database=nextraceone_obs
/// </summary>
public sealed class ClickHouseAnalyticsReader : IClickHouseAnalyticsReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly AnalyticsOptions _options;
    private readonly ILogger<ClickHouseAnalyticsReader> _logger;

    public ClickHouseAnalyticsReader(
        HttpClient httpClient,
        IOptions<AnalyticsOptions> options,
        ILogger<ClickHouseAnalyticsReader> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TraceLatencySummary>> GetTraceLatencySummaryAsync(
        string serviceName,
        string environment,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
    {
        var sql = $"""
            SELECT
                service_name,
                environment,
                quantile(0.50)(duration_ms) AS p50_ms,
                quantile(0.95)(duration_ms) AS p95_ms,
                quantile(0.99)(duration_ms) AS p99_ms,
                count() AS sample_count,
                toStartOfHour(timestamp) AS period_start
            FROM obs_traces
            WHERE service_name = '{EscapeSql(serviceName)}'
              AND environment = '{EscapeSql(environment)}'
              AND timestamp >= '{FormatTs(from)}'
              AND timestamp < '{FormatTs(to)}'
            GROUP BY service_name, environment, period_start
            ORDER BY period_start
            FORMAT JSONEachRow
            """;

        var rows = await ExecuteQueryAsync(sql, ct);
        var result = new List<TraceLatencySummary>();

        foreach (var row in rows)
        {
            try
            {
                result.Add(new TraceLatencySummary(
                    ServiceName: row.GetProperty("service_name").GetString() ?? serviceName,
                    Environment: row.GetProperty("environment").GetString() ?? environment,
                    P50Ms: GetDouble(row, "p50_ms"),
                    P95Ms: GetDouble(row, "p95_ms"),
                    P99Ms: GetDouble(row, "p99_ms"),
                    SampleCount: GetLong(row, "sample_count"),
                    PeriodStart: GetDateTimeOffset(row, "period_start")));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ClickHouseAnalyticsReader: Failed to parse TraceLatencySummary row.");
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MetricAggregation>> GetMetricAggregationAsync(
        string metricName,
        string serviceName,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
    {
        var sql = $"""
            SELECT
                metric_name,
                service_name,
                sum(value) AS sum,
                avg(value) AS avg,
                max(value) AS max,
                count() AS data_points,
                toStartOfHour(timestamp) AS period_start
            FROM obs_metrics
            WHERE metric_name = '{EscapeSql(metricName)}'
              AND service_name = '{EscapeSql(serviceName)}'
              AND timestamp >= '{FormatTs(from)}'
              AND timestamp < '{FormatTs(to)}'
            GROUP BY metric_name, service_name, period_start
            ORDER BY period_start
            FORMAT JSONEachRow
            """;

        var rows = await ExecuteQueryAsync(sql, ct);
        var result = new List<MetricAggregation>();

        foreach (var row in rows)
        {
            try
            {
                result.Add(new MetricAggregation(
                    MetricName: row.GetProperty("metric_name").GetString() ?? metricName,
                    ServiceName: row.GetProperty("service_name").GetString() ?? serviceName,
                    Sum: GetDouble(row, "sum"),
                    Avg: GetDouble(row, "avg"),
                    Max: GetDouble(row, "max"),
                    DataPoints: GetLong(row, "data_points"),
                    PeriodStart: GetDateTimeOffset(row, "period_start")));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ClickHouseAnalyticsReader: Failed to parse MetricAggregation row.");
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<long> GetLogCountAsync(
        string serviceName,
        string level,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
    {
        var sql = $"""
            SELECT count() AS log_count
            FROM obs_logs
            WHERE service_name = '{EscapeSql(serviceName)}'
              AND level = '{EscapeSql(level)}'
              AND timestamp >= '{FormatTs(from)}'
              AND timestamp < '{FormatTs(to)}'
            FORMAT JSONEachRow
            """;

        var rows = await ExecuteQueryAsync(sql, ct);
        return rows.Count > 0 ? GetLong(rows[0], "log_count") : 0L;
    }

    private async Task<IReadOnlyList<JsonElement>> ExecuteQueryAsync(string sql, CancellationToken ct)
    {
        var endpoint = BuildEndpoint();
        var content = new StringContent(sql, Encoding.UTF8, "text/plain");

        try
        {
            var response = await _httpClient.PostAsync(endpoint, content, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "ClickHouseAnalyticsReader: Query failed with status {Status}. Body: {Body}",
                    (int)response.StatusCode, body);
                return [];
            }

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            return ParseJsonEachRow(responseBody);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "ClickHouseAnalyticsReader: HTTP error executing ClickHouse query.");
            return [];
        }
    }

    private string BuildEndpoint()
    {
        var baseUrl = _options.ConnectionString.TrimEnd('/');
        return baseUrl.Contains('?') ? baseUrl : $"{baseUrl}/?";
    }

    private static IReadOnlyList<JsonElement> ParseJsonEachRow(string body)
    {
        var result = new List<JsonElement>();
        foreach (var line in body.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                result.Add(doc.RootElement.Clone());
            }
            catch (JsonException)
            {
                // Ignorar linhas que não são JSON válido
            }
        }
        return result;
    }

    private static double GetDouble(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v)) return 0;
        return v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0;
    }

    private static long GetLong(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v)) return 0;
        if (v.ValueKind == JsonValueKind.Number) return v.GetInt64();
        if (v.ValueKind == JsonValueKind.String && long.TryParse(v.GetString(), out var parsed)) return parsed;
        return 0;
    }

    private static DateTimeOffset GetDateTimeOffset(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v)) return DateTimeOffset.UtcNow;
        var str = v.GetString();
        return DateTimeOffset.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt)
            ? dt
            : DateTimeOffset.UtcNow;
    }

    private static string EscapeSql(string value) =>
        value.Replace("'", "''").Replace("\\", "\\\\");

    private static string FormatTs(DateTimeOffset ts) =>
        ts.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
}
