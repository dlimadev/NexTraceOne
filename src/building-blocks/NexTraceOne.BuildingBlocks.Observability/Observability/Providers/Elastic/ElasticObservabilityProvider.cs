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

namespace NexTraceOne.BuildingBlocks.Observability.Observability.Providers.Elastic;

/// <summary>
/// Implementação do IObservabilityProvider para Elastic.
/// Encapsula leitura de logs, traces e métricas do Elasticsearch/OpenSearch
/// como storage analítico da plataforma NexTraceOne.
///
/// Registrado via DI quando Telemetry:ObservabilityProvider:Provider = "Elastic".
///
/// Prioriza integração com stack Elastic já existente na empresa,
/// evitando duplicação desnecessária de infraestrutura.
///
/// Utiliza a Search API nativa do Elasticsearch (POST /{index}/_search)
/// com query DSL construída a partir dos filtros do produto.
/// O HttpClient é injetado via DI com timeout configurável.
/// </summary>
public sealed class ElasticObservabilityProvider : IObservabilityProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ElasticProviderOptions _elasticOptions;
    private readonly ILogger<ElasticObservabilityProvider> _logger;

    public ElasticObservabilityProvider(
        HttpClient httpClient,
        IOptions<TelemetryStoreOptions> options,
        ILogger<ElasticObservabilityProvider> logger)
    {
        _httpClient = httpClient;
        _elasticOptions = options.Value.ObservabilityProvider.Elastic;
        _logger = logger;
    }

    /// <inheritdoc />
    public string ProviderName => "Elastic";

    /// <inheritdoc />
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        if (!_elasticOptions.Enabled)
            return false;

        if (string.IsNullOrWhiteSpace(_elasticOptions.Endpoint))
            return false;

        try
        {
            var endpoint = _elasticOptions.Endpoint.TrimEnd('/');
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"{endpoint}/_cluster/health"));
            ApplyAuthentication(request);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return false;

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(body);
            var status = doc.RootElement.GetProperty("status").GetString();

            // Elastic cluster status: green, yellow, or red. green and yellow are acceptable.
            return status is "green" or "yellow";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Elastic health check failed: {Message}", ex.Message);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LogEntry>> QueryLogsAsync(
        LogQueryFilter filter,
        CancellationToken cancellationToken = default)
    {
        var musts = new List<object>
        {
            new { range = new Dictionary<string, object> { ["@timestamp"] = new { gte = filter.From.ToString("o"), lte = filter.Until.ToString("o") } } },
            new { term = new Dictionary<string, object> { ["resource.environment"] = filter.Environment } }
        };

        if (!string.IsNullOrWhiteSpace(filter.ServiceName))
            musts.Add(new { term = new Dictionary<string, object> { ["resource.service.name"] = filter.ServiceName } });

        if (!string.IsNullOrWhiteSpace(filter.Level))
            musts.Add(new { term = new Dictionary<string, object> { ["severity_text"] = filter.Level } });

        if (!string.IsNullOrWhiteSpace(filter.MessageContains))
            musts.Add(new { match = new Dictionary<string, object> { ["body"] = filter.MessageContains } });

        if (!string.IsNullOrWhiteSpace(filter.TraceId))
            musts.Add(new { term = new Dictionary<string, object> { ["trace_id"] = filter.TraceId } });

        var query = new
        {
            size = filter.Limit,
            sort = new object[] { new Dictionary<string, object> { ["@timestamp"] = new { order = "desc" } } },
            query = new { @bool = new { must = musts } }
        };

        var indexPattern = $"{_elasticOptions.IndexPrefix}-logs-*";
        var hits = await SearchAsync(indexPattern, query, cancellationToken);

        var results = new List<LogEntry>();
        foreach (var hit in hits)
        {
            if (!hit.TryGetProperty("_source", out var src))
                continue;

            results.Add(new LogEntry
            {
                Timestamp = GetDateTimeOffset(src, "@timestamp"),
                Environment = filter.Environment,
                ServiceName = GetNestedString(src, "resource", "service.name") ?? GetString(src, "service_name") ?? "",
                ApplicationName = GetString(src, "application_name"),
                ModuleName = GetString(src, "module_name"),
                Level = GetString(src, "severity_text") ?? "Information",
                Message = GetString(src, "body") ?? "",
                Exception = GetString(src, "exception"),
                TraceId = GetString(src, "trace_id"),
                SpanId = GetString(src, "span_id"),
                CorrelationId = GetString(src, "correlation_id"),
                HostName = GetNestedString(src, "resource", "host.name") ?? GetString(src, "host_name"),
                ContainerName = GetNestedString(src, "resource", "container.name"),
                Attributes = GetStringDictionary(src, "attributes")
            });
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TraceSummary>> QueryTracesAsync(
        TraceQueryFilter filter,
        CancellationToken cancellationToken = default)
    {
        var musts = new List<object>
        {
            new { range = new Dictionary<string, object> { ["start_time"] = new { gte = filter.From.ToString("o"), lte = filter.Until.ToString("o") } } },
            new { term = new Dictionary<string, object> { ["resource.environment"] = filter.Environment } }
        };

        if (!string.IsNullOrWhiteSpace(filter.ServiceName))
            musts.Add(new { term = new Dictionary<string, object> { ["resource.service.name"] = filter.ServiceName } });

        if (!string.IsNullOrWhiteSpace(filter.OperationName))
            musts.Add(new { term = new Dictionary<string, object> { ["name"] = filter.OperationName } });

        if (filter.MinDurationMs.HasValue)
            musts.Add(new { range = new Dictionary<string, object> { ["duration_ms"] = new { gte = filter.MinDurationMs.Value } } });

        if (filter.HasErrors == true)
            musts.Add(new { term = new Dictionary<string, object> { ["status.code"] = "Error" } });

        if (!string.IsNullOrWhiteSpace(filter.ServiceKind))
        {
            var kindCondition = ElasticServiceKindFilter.Build(filter.ServiceKind);
            if (kindCondition is not null)
                musts.Add(kindCondition);
        }

        // Query root spans only (no parent span) for trace summaries
        var mustNots = new List<object>
        {
            new { exists = new { field = "parent_span_id" } }
        };

        var query = new
        {
            size = filter.Limit,
            sort = new object[] { new Dictionary<string, object> { ["start_time"] = new { order = "desc" } } },
            query = new { @bool = new { must = musts, must_not = mustNots } }
        };

        var indexPattern = $"{_elasticOptions.IndexPrefix}-traces-*";
        var hits = await SearchAsync(indexPattern, query, cancellationToken);

        var results = new List<TraceSummary>();
        foreach (var hit in hits)
        {
            if (!hit.TryGetProperty("_source", out var src))
                continue;

            var spanKind = GetString(src, "span_kind");
            var spanAttrs = GetStringDictionary(src, "attributes");

            results.Add(new TraceSummary
            {
                TraceId = GetString(src, "trace_id") ?? "",
                ServiceName = GetNestedString(src, "resource", "service.name") ?? GetString(src, "service_name") ?? "",
                OperationName = GetString(src, "name") ?? "",
                StartTime = GetDateTimeOffset(src, "start_time"),
                DurationMs = GetDouble(src, "duration_ms"),
                StatusCode = GetString(src, "status.code") ?? GetNestedString(src, "status", "code"),
                Environment = filter.Environment,
                SpanCount = GetInt(src, "span_count"),
                HasErrors = (GetString(src, "status.code") ?? GetNestedString(src, "status", "code")) == "Error",
                RootServiceKind = SpanKindResolver.Resolve(spanAttrs, spanKind)
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

        var query = new
        {
            size = 1000,
            sort = new object[] { new Dictionary<string, object> { ["start_time"] = new { order = "asc" } } },
            query = new { term = new Dictionary<string, object> { ["trace_id"] = traceId } }
        };

        var indexPattern = $"{_elasticOptions.IndexPrefix}-traces-*";
        var hits = await SearchAsync(indexPattern, query, cancellationToken);

        if (hits.Count == 0)
            return null;

        var spans = new List<SpanDetail>();
        var services = new HashSet<string>();
        double maxDuration = 0;

        foreach (var hit in hits)
        {
            if (!hit.TryGetProperty("_source", out var src))
                continue;

            var serviceName = GetNestedString(src, "resource", "service.name") ?? GetString(src, "service_name") ?? "";
            services.Add(serviceName);

            var startTime = GetDateTimeOffset(src, "start_time");
            var endTime = GetDateTimeOffset(src, "end_time");
            var durationMs = GetDouble(src, "duration_ms");

            if (durationMs <= 0 && endTime > startTime)
                durationMs = (endTime - startTime).TotalMilliseconds;

            var spanKind = GetString(src, "span_kind");
            var spanAttrs = GetStringDictionary(src, "attributes");

            spans.Add(new SpanDetail
            {
                TraceId = traceId,
                SpanId = GetString(src, "span_id") ?? "",
                ParentSpanId = GetString(src, "parent_span_id"),
                ServiceName = serviceName,
                OperationName = GetString(src, "name") ?? "",
                StartTime = startTime,
                EndTime = endTime,
                DurationMs = durationMs,
                StatusCode = GetString(src, "status.code") ?? GetNestedString(src, "status", "code"),
                StatusMessage = GetString(src, "status.message") ?? GetNestedString(src, "status", "message"),
                Environment = GetNestedString(src, "resource", "environment") ?? "",
                SpanKind = spanKind,
                ServiceKind = SpanKindResolver.Resolve(spanAttrs, spanKind),
                ResourceAttributes = GetStringDictionary(src, "resource"),
                SpanAttributes = spanAttrs,
                Events = ParseSpanEvents(src)
            });

            if (durationMs > maxDuration)
                maxDuration = durationMs;
        }

        // Compute total duration from root span if available
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
        var musts = new List<object>
        {
            new { range = new Dictionary<string, object> { ["@timestamp"] = new { gte = filter.From.ToString("o"), lte = filter.Until.ToString("o") } } },
            new { term = new Dictionary<string, object> { ["resource.environment"] = filter.Environment } },
            new { term = new Dictionary<string, object> { ["metric_name"] = filter.MetricName } }
        };

        if (!string.IsNullOrWhiteSpace(filter.ServiceName))
            musts.Add(new { term = new Dictionary<string, object> { ["resource.service.name"] = filter.ServiceName } });

        var query = new
        {
            size = 1000,
            sort = new object[] { new Dictionary<string, object> { ["@timestamp"] = new { order = "asc" } } },
            query = new { @bool = new { must = musts } }
        };

        var indexPattern = $"{_elasticOptions.IndexPrefix}-metrics-*";
        var hits = await SearchAsync(indexPattern, query, cancellationToken);

        var results = new List<TelemetryMetricPoint>();
        foreach (var hit in hits)
        {
            if (!hit.TryGetProperty("_source", out var src))
                continue;

            results.Add(new TelemetryMetricPoint
            {
                Timestamp = GetDateTimeOffset(src, "@timestamp"),
                MetricName = GetString(src, "metric_name") ?? filter.MetricName,
                Value = GetDouble(src, "value"),
                ServiceName = GetNestedString(src, "resource", "service.name") ?? GetString(src, "service_name") ?? "",
                Environment = filter.Environment,
                Labels = GetStringDictionary(src, "labels")
            });
        }

        return results;
    }

    /// <summary>
    /// Executa uma query de pesquisa contra o Elasticsearch e retorna os hits.
    /// POST {endpoint}/{indexPattern}/_search
    /// </summary>
    private async Task<List<JsonElement>> SearchAsync(
        string indexPattern,
        object queryBody,
        CancellationToken cancellationToken)
    {
        try
        {
            var endpoint = _elasticOptions.Endpoint.TrimEnd('/');
            var requestUri = new Uri($"{endpoint}/{indexPattern}/_search");
            var json = JsonSerializer.Serialize(queryBody, JsonOptions);

            using var content = new StringContent(json, Encoding.UTF8);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Content = content;
            ApplyAuthentication(request);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var sanitized = errorBody.Length > 300 ? errorBody[..300] + " [truncated]" : errorBody;
                _logger.LogWarning(
                    "Elasticsearch query to {Index} failed: HTTP {StatusCode} — {Body}",
                    indexPattern, (int)response.StatusCode, sanitized);
                return [];
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseBody);

            if (!doc.RootElement.TryGetProperty("hits", out var hitsWrapper))
                return [];

            if (!hitsWrapper.TryGetProperty("hits", out var hitsArray))
                return [];

            // Clone elements because the JsonDocument will be disposed
            var results = new List<JsonElement>();
            foreach (var hit in hitsArray.EnumerateArray())
                results.Add(hit.Clone());

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Elasticsearch query to {Index} failed: {Message}",
                indexPattern, ex.Message);
            return [];
        }
    }

    /// <summary>
    /// Aplica autenticação ApiKey ao request HTTP quando configurada.
    /// </summary>
    private void ApplyAuthentication(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(_elasticOptions.ApiKey))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("ApiKey", _elasticOptions.ApiKey);
        }
    }

    // --- JSON helper methods for safe extraction from JsonElement ---

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return null;
    }

    private static string? GetNestedString(JsonElement element, string parentProperty, string childProperty)
    {
        if (element.TryGetProperty(parentProperty, out var parent) && parent.ValueKind == JsonValueKind.Object)
            return GetString(parent, childProperty);
        return null;
    }

    private static DateTimeOffset GetDateTimeOffset(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(prop.GetString(), out var dto))
                return dto;
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

    private static int GetInt(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.Number)
                return prop.GetInt32();
        }
        return 0;
    }

    private static IReadOnlyDictionary<string, string>? GetStringDictionary(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var prop) || prop.ValueKind != JsonValueKind.Object)
            return null;

        var dict = new Dictionary<string, string>();
        foreach (var p in prop.EnumerateObject())
        {
            dict[p.Name] = p.Value.ValueKind == JsonValueKind.String
                ? p.Value.GetString() ?? ""
                : p.Value.ToString();
        }
        return dict.Count > 0 ? dict : null;
    }

    private static IReadOnlyList<SpanEvent>? ParseSpanEvents(JsonElement source)
    {
        if (!source.TryGetProperty("events", out var eventsArray) || eventsArray.ValueKind != JsonValueKind.Array)
            return null;

        var events = new List<SpanEvent>();
        foreach (var ev in eventsArray.EnumerateArray())
        {
            events.Add(new SpanEvent
            {
                Name = GetString(ev, "name") ?? "",
                Timestamp = GetDateTimeOffset(ev, "timestamp"),
                Attributes = GetStringDictionary(ev, "attributes")
            });
        }
        return events.Count > 0 ? events : null;
    }
}

/// <summary>
/// Health check para o provider Elastic.
/// Verifica conectividade e disponibilidade do Elasticsearch.
/// </summary>
public sealed class ElasticHealthCheck : IHealthCheck
{
    private readonly ElasticObservabilityProvider _provider;

    public ElasticHealthCheck(ElasticObservabilityProvider provider)
    {
        _provider = provider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var isHealthy = await _provider.IsHealthyAsync(cancellationToken);
        return isHealthy
            ? HealthCheckResult.Healthy("Elastic observability provider is available")
            : HealthCheckResult.Degraded("Elastic observability provider is not available");
    }
}
