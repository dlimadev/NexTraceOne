using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexTraceOne.BuildingBlocks.Observability.Observability.Abstractions;
using NexTraceOne.BuildingBlocks.Observability.Observability.Models;
using NexTraceOne.BuildingBlocks.Observability.Telemetry.Configuration;

namespace NexTraceOne.BuildingBlocks.Observability.Observability.Providers.ClickHouse;

/// <summary>
/// Implementação do IObservabilityProvider para ClickHouse.
/// Encapsula leitura de logs, traces e métricas do ClickHouse
/// como storage analítico da plataforma NexTraceOne.
///
/// Registrado via DI quando Telemetry:ObservabilityProvider:Provider = "ClickHouse".
///
/// Utiliza o protocolo HTTP nativo do ClickHouse (porta 8123)
/// com formato JSONEachRow para resultados de queries SELECT.
/// O HttpClient é injetado via DI com timeout configurável.
/// </summary>
public sealed class ClickHouseObservabilityProvider : IObservabilityProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    private static readonly System.Text.RegularExpressions.Regex ValidIdentifierPattern =
        new(@"^[a-zA-Z_][a-zA-Z0-9_]*$", System.Text.RegularExpressions.RegexOptions.Compiled);

    private readonly HttpClient _httpClient;
    private readonly ClickHouseProviderOptions _clickHouseOptions;
    private readonly ILogger<ClickHouseObservabilityProvider> _logger;
    private readonly string _database;

    public ClickHouseObservabilityProvider(
        HttpClient httpClient,
        IOptions<TelemetryStoreOptions> options,
        ILogger<ClickHouseObservabilityProvider> logger)
    {
        _httpClient = httpClient;
        _clickHouseOptions = options.Value.ObservabilityProvider.ClickHouse;
        _logger = logger;

        // Extract database name from structured connection string (Host=...;Port=...;Database=...;)
        var db = ExtractDatabaseFromConnectionString(_clickHouseOptions.ConnectionString);

        // Validate database name at construction time to prevent SQL injection via configuration
        if (string.IsNullOrWhiteSpace(db) || !ValidIdentifierPattern.IsMatch(db))
            throw new ArgumentException($"Invalid ClickHouse database name: '{db}'. Must contain only alphanumeric characters and underscores.");
        _database = db;
    }

    /// <inheritdoc />
    public string ProviderName => "ClickHouse";

    /// <inheritdoc />
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        if (!_clickHouseOptions.Enabled)
            return false;

        if (string.IsNullOrWhiteSpace(_clickHouseOptions.ConnectionString))
            return false;

        try
        {
            var baseUrl = ResolveHttpEndpoint();
            var requestUri = new Uri($"{baseUrl}/?query=" + Uri.EscapeDataString("SELECT 1 FORMAT JSONEachRow"));
            var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ClickHouse health check failed: {Message}", ex.Message);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LogEntry>> QueryLogsAsync(
        LogQueryFilter filter,
        CancellationToken cancellationToken = default)
    {
        var db = _database;
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture,
            $"SELECT Timestamp, ServiceName, SeverityText, Body, TraceId, SpanId, " +
            $"ResourceAttributes, LogAttributes " +
            $"FROM {db}.otel_logs " +
            $"WHERE Timestamp >= '{FormatDateTime(filter.From)}' " +
            $"AND Timestamp <= '{FormatDateTime(filter.Until)}'");

        AppendCondition(sb, "ResourceAttributes['deployment.environment']", filter.Environment);

        if (!string.IsNullOrWhiteSpace(filter.ServiceName))
            AppendCondition(sb, "ServiceName", filter.ServiceName);

        if (!string.IsNullOrWhiteSpace(filter.Level))
            AppendCondition(sb, "SeverityText", filter.Level);

        if (!string.IsNullOrWhiteSpace(filter.MessageContains))
            sb.Append(CultureInfo.InvariantCulture, $" AND Body LIKE '%{EscapeLikePattern(filter.MessageContains)}%'");

        if (!string.IsNullOrWhiteSpace(filter.TraceId))
            AppendCondition(sb, "TraceId", filter.TraceId);

        sb.Append(" ORDER BY Timestamp DESC");
        sb.Append(CultureInfo.InvariantCulture, $" LIMIT {filter.Limit}");

        var rows = await QueryAsync(sb.ToString(), cancellationToken);

        var results = new List<LogEntry>();
        foreach (var row in rows)
        {
            results.Add(new LogEntry
            {
                Timestamp = GetDateTimeOffset(row, "Timestamp"),
                Environment = filter.Environment,
                ServiceName = GetString(row, "ServiceName") ?? "",
                Level = GetString(row, "SeverityText") ?? "Information",
                Message = GetString(row, "Body") ?? "",
                TraceId = GetString(row, "TraceId"),
                SpanId = GetString(row, "SpanId"),
                Attributes = GetStringDictionary(row, "LogAttributes")
            });
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TraceSummary>> QueryTracesAsync(
        TraceQueryFilter filter,
        CancellationToken cancellationToken = default)
    {
        var db = _database;
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture,
            $"SELECT TraceId, ServiceName, SpanName, Timestamp, Duration, " +
            $"StatusCode, ResourceAttributes, SpanAttributes, ParentSpanId " +
            $"FROM {db}.otel_traces " +
            $"WHERE Timestamp >= '{FormatDateTime(filter.From)}' " +
            $"AND Timestamp <= '{FormatDateTime(filter.Until)}'");

        AppendCondition(sb, "ResourceAttributes['deployment.environment']", filter.Environment);

        if (!string.IsNullOrWhiteSpace(filter.ServiceName))
            AppendCondition(sb, "ServiceName", filter.ServiceName);

        if (!string.IsNullOrWhiteSpace(filter.OperationName))
            AppendCondition(sb, "SpanName", filter.OperationName);

        if (filter.MinDurationMs.HasValue)
        {
            // ClickHouse stores Duration in nanoseconds; convert from ms
            var minNanos = (long)(filter.MinDurationMs.Value * 1_000_000);
            sb.Append(CultureInfo.InvariantCulture, $" AND Duration >= {minNanos}");
        }

        if (filter.HasErrors == true)
            AppendCondition(sb, "StatusCode", "Error");

        // Root spans only for summaries
        sb.Append(" AND ParentSpanId = ''");

        sb.Append(" ORDER BY Timestamp DESC");
        sb.Append(CultureInfo.InvariantCulture, $" LIMIT {filter.Limit}");

        var rows = await QueryAsync(sb.ToString(), cancellationToken);

        var results = new List<TraceSummary>();
        foreach (var row in rows)
        {
            var durationNanos = GetLong(row, "Duration");
            var durationMs = durationNanos / 1_000_000.0;
            var statusCode = GetString(row, "StatusCode");

            results.Add(new TraceSummary
            {
                TraceId = GetString(row, "TraceId") ?? "",
                ServiceName = GetString(row, "ServiceName") ?? "",
                OperationName = GetString(row, "SpanName") ?? "",
                StartTime = GetDateTimeOffset(row, "Timestamp"),
                DurationMs = durationMs,
                StatusCode = statusCode,
                Environment = filter.Environment,
                HasErrors = string.Equals(statusCode, "Error", StringComparison.OrdinalIgnoreCase)
            });
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<TraceDetail?> GetTraceDetailAsync(
        string traceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(traceId))
            return null;

        var db = _database;
        var query = $"SELECT TraceId, SpanId, ParentSpanId, ServiceName, SpanName, " +
                    $"Timestamp, Duration, StatusCode, StatusMessage, " +
                    $"ResourceAttributes, SpanAttributes, Events.Name, Events.Timestamp " +
                    $"FROM {db}.otel_traces " +
                    $"WHERE TraceId = '{EscapeSqlString(traceId)}' " +
                    $"ORDER BY Timestamp ASC " +
                    $"LIMIT 1000";

        var rows = await QueryAsync(query, cancellationToken);

        if (rows.Count == 0)
            return null;

        var spans = new List<SpanDetail>();
        var services = new HashSet<string>();
        double maxDuration = 0;

        foreach (var row in rows)
        {
            var serviceName = GetString(row, "ServiceName") ?? "";
            services.Add(serviceName);

            var startTime = GetDateTimeOffset(row, "Timestamp");
            var durationNanos = GetLong(row, "Duration");
            var durationMs = durationNanos / 1_000_000.0;
            var endTime = startTime.AddMilliseconds(durationMs);

            var environment = "";
            var resourceAttrs = GetStringDictionary(row, "ResourceAttributes");
            if (resourceAttrs != null && resourceAttrs.TryGetValue("deployment.environment", out var env))
                environment = env;

            spans.Add(new SpanDetail
            {
                TraceId = traceId,
                SpanId = GetString(row, "SpanId") ?? "",
                ParentSpanId = NullIfEmpty(GetString(row, "ParentSpanId")),
                ServiceName = serviceName,
                OperationName = GetString(row, "SpanName") ?? "",
                StartTime = startTime,
                EndTime = endTime,
                DurationMs = durationMs,
                StatusCode = GetString(row, "StatusCode"),
                StatusMessage = GetString(row, "StatusMessage"),
                Environment = environment,
                ResourceAttributes = resourceAttrs,
                SpanAttributes = GetStringDictionary(row, "SpanAttributes"),
                Events = ParseClickHouseSpanEvents(row)
            });

            if (durationMs > maxDuration)
                maxDuration = durationMs;
        }

        var rootSpan = spans.FirstOrDefault(s => string.IsNullOrEmpty(s.ParentSpanId));
        var totalDuration = rootSpan?.DurationMs ?? maxDuration;

        return new TraceDetail
        {
            TraceId = traceId,
            Spans = spans,
            DurationMs = totalDuration,
            Services = services.ToList()
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TelemetryMetricPoint>> QueryMetricsAsync(
        MetricQueryFilter filter,
        CancellationToken cancellationToken = default)
    {
        var db = _database;
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture,
            $"SELECT TimeUnix, MetricName, Value, ServiceName, ResourceAttributes, Attributes " +
            $"FROM {db}.otel_metrics " +
            $"WHERE TimeUnix >= '{FormatDateTime(filter.From)}' " +
            $"AND TimeUnix <= '{FormatDateTime(filter.Until)}'");

        AppendCondition(sb, "ResourceAttributes['deployment.environment']", filter.Environment);
        AppendCondition(sb, "MetricName", filter.MetricName);

        if (!string.IsNullOrWhiteSpace(filter.ServiceName))
            AppendCondition(sb, "ServiceName", filter.ServiceName);

        sb.Append(" ORDER BY TimeUnix ASC");
        sb.Append(" LIMIT 1000");

        var rows = await QueryAsync(sb.ToString(), cancellationToken);

        var results = new List<TelemetryMetricPoint>();
        foreach (var row in rows)
        {
            results.Add(new TelemetryMetricPoint
            {
                Timestamp = GetDateTimeOffset(row, "TimeUnix"),
                MetricName = GetString(row, "MetricName") ?? filter.MetricName,
                Value = GetDouble(row, "Value"),
                ServiceName = GetString(row, "ServiceName") ?? "",
                Environment = filter.Environment,
                Labels = GetStringDictionary(row, "Attributes")
            });
        }

        return results;
    }

    /// <summary>
    /// Executa uma query SELECT contra o ClickHouse via HTTP interface
    /// e retorna as linhas como lista de JsonElement (formato JSONEachRow).
    /// </summary>
    private async Task<List<JsonElement>> QueryAsync(
        string sql,
        CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = ResolveHttpEndpoint();
            var fullQuery = sql + " FORMAT JSONEachRow";
            var requestUri = new Uri($"{baseUrl}/?query=" + Uri.EscapeDataString(fullQuery));

            var response = await _httpClient.GetAsync(requestUri, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var sanitized = errorBody.Length > 300 ? errorBody[..300] + " [truncated]" : errorBody;
                _logger.LogWarning(
                    "ClickHouse query failed: HTTP {StatusCode} — {Body}",
                    (int)response.StatusCode, sanitized);
                return [];
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(responseBody))
                return [];

            // JSONEachRow format: one JSON object per line
            var results = new List<JsonElement>();
            foreach (var line in responseBody.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    results.Add(doc.RootElement.Clone());
                }
                catch (JsonException)
                {
                    // Skip malformed lines
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ClickHouse query failed: {Message}", ex.Message);
            return [];
        }
    }

    /// <summary>
    /// Extrai o nome da base de dados a partir da connection string estruturada.
    /// Formato esperado: Host=clickhouse;Port=8123;Database=nextraceone_obs;...
    /// </summary>
    private static string ExtractDatabaseFromConnectionString(string connectionString)
    {
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0].Trim(), p => p[1].Trim(), StringComparer.OrdinalIgnoreCase);

        return parts.GetValueOrDefault("Database") ?? "nextraceone_obs";
    }

    /// <summary>
    /// Resolve o endpoint HTTP do ClickHouse a partir da connection string.
    /// Formato esperado: Host=clickhouse;Port=8123;Database=nextraceone_obs;...
    /// Resultado: http://clickhouse:8123
    /// </summary>
    private string ResolveHttpEndpoint()
    {
        var connStr = _clickHouseOptions.ConnectionString;

        // Try parsing as a structured connection string
        var parts = connStr.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0].Trim(), p => p[1].Trim(), StringComparer.OrdinalIgnoreCase);

        var host = parts.GetValueOrDefault("Host") ?? "localhost";
        var port = parts.GetValueOrDefault("Port") ?? "8123";

        return $"http://{host}:{port}";
    }

    /// <summary>
    /// Formata um DateTimeOffset no formato aceite pelo ClickHouse DateTime64 UTC.
    /// Exemplo: "2026-03-25 21:00:00.000"
    /// </summary>
    private static string FormatDateTime(DateTimeOffset dt)
        => dt.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);

    /// <summary>
    /// Escapa aspas simples e barras invertidas numa string para uso seguro em queries SQL do ClickHouse.
    /// Protege contra SQL injection em valores fornecidos pelo utilizador.
    /// </summary>
    private static string EscapeSqlString(string value)
        => value.Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("'", "\\'", StringComparison.Ordinal);

    /// <summary>
    /// Escapa caracteres especiais do operador LIKE do ClickHouse (% e _) além do escape SQL padrão.
    /// Usado quando o valor do utilizador é inserido dentro de um padrão LIKE.
    /// </summary>
    private static string EscapeLikePattern(string value)
        => EscapeSqlString(value)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

    /// <summary>
    /// Adiciona uma condição de igualdade ao StringBuilder da query SQL.
    /// </summary>
    private static void AppendCondition(StringBuilder sb, string column, string value)
        => sb.Append(CultureInfo.InvariantCulture, $" AND {column} = '{EscapeSqlString(value)}'");

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;

    // --- JSON helper methods for safe extraction from JsonElement ---

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return null;
    }

    private static DateTimeOffset GetDateTimeOffset(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(prop.GetString(), out var dto))
                return dto;
            if (prop.ValueKind == JsonValueKind.Number)
                return DateTimeOffset.FromUnixTimeSeconds(prop.GetInt64());
        }
        return DateTimeOffset.UtcNow;
    }

    private static double GetDouble(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.Number)
                return prop.GetDouble();
            if (prop.ValueKind == JsonValueKind.String && double.TryParse(prop.GetString(), out var d))
                return d;
        }
        return 0;
    }

    private static long GetLong(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.Number)
                return prop.GetInt64();
            if (prop.ValueKind == JsonValueKind.String && long.TryParse(prop.GetString(), out var l))
                return l;
        }
        return 0;
    }

    private static IReadOnlyDictionary<string, string>? GetStringDictionary(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var prop))
            return null;

        if (prop.ValueKind == JsonValueKind.Object)
        {
            var dict = new Dictionary<string, string>();
            foreach (var p in prop.EnumerateObject())
            {
                dict[p.Name] = p.Value.ValueKind == JsonValueKind.String
                    ? p.Value.GetString() ?? ""
                    : p.Value.ToString();
            }
            return dict.Count > 0 ? dict : null;
        }

        // ClickHouse Map type can be returned as JSON string in some configurations
        if (prop.ValueKind == JsonValueKind.String)
        {
            try
            {
                var str = prop.GetString();
                if (!string.IsNullOrWhiteSpace(str))
                {
                    var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(str);
                    return parsed is { Count: > 0 } ? parsed : null;
                }
            }
            catch (JsonException)
            {
                // Not a valid JSON map
            }
        }

        return null;
    }

    /// <summary>
    /// Parses ClickHouse nested array columns Events.Name and Events.Timestamp
    /// into a list of SpanEvent objects.
    /// </summary>
    private static IReadOnlyList<SpanEvent>? ParseClickHouseSpanEvents(JsonElement row)
    {
        if (!row.TryGetProperty("Events.Name", out var namesArray) || namesArray.ValueKind != JsonValueKind.Array)
            return null;

        row.TryGetProperty("Events.Timestamp", out var timestampsArray);

        var events = new List<SpanEvent>();
        var index = 0;
        foreach (var name in namesArray.EnumerateArray())
        {
            var eventName = name.GetString() ?? "";
            var timestamp = DateTimeOffset.UtcNow;

            if (timestampsArray.ValueKind == JsonValueKind.Array)
            {
                var tsArr = timestampsArray.EnumerateArray().ToList();
                if (index < tsArr.Count && tsArr[index].ValueKind == JsonValueKind.String &&
                    DateTimeOffset.TryParse(tsArr[index].GetString(), out var ts))
                {
                    timestamp = ts;
                }
            }

            events.Add(new SpanEvent
            {
                Name = eventName,
                Timestamp = timestamp
            });

            index++;
        }

        return events.Count > 0 ? events : null;
    }
}

/// <summary>
/// Health check para o provider ClickHouse.
/// Verifica conectividade e disponibilidade do ClickHouse.
/// </summary>
public sealed class ClickHouseHealthCheck : IHealthCheck
{
    private readonly ClickHouseObservabilityProvider _provider;

    public ClickHouseHealthCheck(ClickHouseObservabilityProvider provider)
    {
        _provider = provider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var isHealthy = await _provider.IsHealthyAsync(cancellationToken);
        return isHealthy
            ? HealthCheckResult.Healthy("ClickHouse observability provider is available")
            : HealthCheckResult.Degraded("ClickHouse observability provider is not available");
    }
}
