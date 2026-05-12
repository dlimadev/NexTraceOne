using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexTraceOne.BuildingBlocks.Observability.Telemetry.Configuration;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.SearchLogs;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Services;

/// <summary>
/// Implementação de <see cref="ILogSearchService"/> sobre Elasticsearch.
/// Constrói query DSL bool/must com filtros de tenant, serviço, severidade,
/// ambiente e texto livre. Suporta janela temporal configurável.
/// SaaS-07: Log Search UI.
/// </summary>
internal sealed class ElasticsearchLogSearchService : ILogSearchService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;
    private readonly ElasticProviderOptions _elasticOptions;
    private readonly ILogger<ElasticsearchLogSearchService> _logger;

    public ElasticsearchLogSearchService(
        HttpClient httpClient,
        IOptions<TelemetryStoreOptions> options,
        ILogger<ElasticsearchLogSearchService> logger)
    {
        _httpClient = httpClient;
        _elasticOptions = options.Value.ObservabilityProvider.Elastic;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<SearchLogs.LogEntry> Entries, long Total)> SearchAsync(
        LogSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (!_elasticOptions.Enabled || string.IsNullOrWhiteSpace(_elasticOptions.Endpoint))
        {
            _logger.LogDebug("Elasticsearch desabilitado ou endpoint não configurado — devolvendo resultados vazios.");
            return ([], 0);
        }

        var index = $"{_elasticOptions.IndexPrefix}-logs-*";
        var endpoint = _elasticOptions.Endpoint.TrimEnd('/');
        var url = $"{endpoint}/{index}/_search";

        var body = BuildQueryBody(request);
        var json = JsonSerializer.Serialize(body, JsonOptions);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(url))
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        ApplyAuthentication(httpRequest);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao contactar Elasticsearch para pesquisa de logs.");
            return ([], 0);
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Elasticsearch devolveu {Status} para pesquisa de logs.", response.StatusCode);
            return ([], 0);
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseResponse(responseJson);
    }

    // ── Construção do query DSL ───────────────────────────────────────────────

    private static object BuildQueryBody(LogSearchRequest req)
    {
        var mustClauses = new List<object>
        {
            new { term = new Dictionary<string, object> { ["tenant_id"] = req.TenantId.ToString() } },
            new
            {
                range = new Dictionary<string, object>
                {
                    ["@timestamp"] = new
                    {
                        gte = req.From.ToString("O"),
                        lte = req.To.ToString("O"),
                    }
                }
            }
        };

        if (!string.IsNullOrWhiteSpace(req.ServiceName))
            mustClauses.Add(new { term = new Dictionary<string, object> { ["service.name"] = req.ServiceName } });

        if (!string.IsNullOrWhiteSpace(req.Severity))
            mustClauses.Add(new { term = new Dictionary<string, object> { ["log.level"] = req.Severity.ToLowerInvariant() } });

        if (!string.IsNullOrWhiteSpace(req.Environment))
            mustClauses.Add(new { term = new Dictionary<string, object> { ["deployment.environment"] = req.Environment } });

        if (!string.IsNullOrWhiteSpace(req.SearchText))
            mustClauses.Add(new { match = new Dictionary<string, object> { ["message"] = req.SearchText } });

        var from = (req.Page - 1) * req.PageSize;

        return new
        {
            query = new { @bool = new { must = mustClauses } },
            from,
            size = req.PageSize,
            sort = new[] { new { @timestamp = new { order = "desc" } } },
        };
    }

    // ── Mapeamento da resposta Elasticsearch ─────────────────────────────────

    private (IReadOnlyList<SearchLogs.LogEntry> Entries, long Total) ParseResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var total = root
                .GetProperty("hits")
                .GetProperty("total")
                .TryGetProperty("value", out var val)
                    ? val.GetInt64()
                    : root.GetProperty("hits").GetProperty("total").GetInt64();

            var hits = root.GetProperty("hits").GetProperty("hits");
            var entries = new List<SearchLogs.LogEntry>();

            foreach (var hit in hits.EnumerateArray())
            {
                var id = hit.GetProperty("_id").GetString() ?? Guid.NewGuid().ToString();
                var source = hit.GetProperty("_source");

                var timestamp = source.TryGetProperty("@timestamp", out var ts)
                    ? DateTimeOffset.Parse(ts.GetString()!)
                    : DateTimeOffset.UtcNow;

                var severity = source.TryGetProperty("log.level", out var sev)
                    ? sev.GetString() ?? "info"
                    : source.TryGetProperty("level", out var lvl)
                        ? lvl.GetString() ?? "info"
                        : "info";

                var message = source.TryGetProperty("message", out var msg)
                    ? msg.GetString() ?? string.Empty
                    : string.Empty;

                var serviceName = source.TryGetProperty("service.name", out var svc)
                    ? svc.GetString()
                    : null;

                var environment = source.TryGetProperty("deployment.environment", out var env)
                    ? env.GetString()
                    : source.TryGetProperty("environment", out var env2)
                        ? env2.GetString()
                        : null;

                var attributes = ExtractAttributes(source);

                entries.Add(new SearchLogs.LogEntry(id, timestamp, severity, message, serviceName, environment, attributes));
            }

            return (entries, total);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao mapear resposta Elasticsearch para entradas de log.");
            return ([], 0);
        }
    }

    private static IReadOnlyDictionary<string, object?> ExtractAttributes(JsonElement source)
    {
        // Campos já mapeados para propriedades estruturadas do LogEntry
        var knownFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "@timestamp", "log.level", "level", "message",
            "service.name", "deployment.environment", "environment",
            "tenant_id",
        };

        var attrs = new Dictionary<string, object?>();

        foreach (var prop in source.EnumerateObject())
        {
            if (knownFields.Contains(prop.Name))
                continue;

            attrs[prop.Name] = prop.Value.ValueKind switch
            {
                JsonValueKind.String => prop.Value.GetString(),
                JsonValueKind.Number when prop.Value.TryGetInt64(out var i) => i,
                JsonValueKind.Number => prop.Value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => prop.Value.GetRawText(),
            };
        }

        return attrs;
    }

    private void ApplyAuthentication(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(_elasticOptions.ApiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("ApiKey", _elasticOptions.ApiKey);
    }
}
