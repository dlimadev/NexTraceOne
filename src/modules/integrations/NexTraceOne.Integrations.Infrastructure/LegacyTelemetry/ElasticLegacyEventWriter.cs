using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexTraceOne.Integrations.Application.LegacyTelemetry.Abstractions;
using NexTraceOne.Integrations.Domain.LegacyTelemetry;

namespace NexTraceOne.Integrations.Infrastructure.LegacyTelemetry;

/// <summary>
/// Writer para persistência de eventos legacy normalizados em Elasticsearch via Bulk API.
/// Índice alvo: nextraceone-obs-mf-operational-events
/// </summary>
public sealed class ElasticLegacyEventWriter : ILegacyEventWriter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ElasticLegacyEventWriter> _logger;
    private readonly ElasticLegacyWriterOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ElasticLegacyEventWriter(
        HttpClient httpClient,
        ILogger<ElasticLegacyEventWriter> logger,
        IOptions<ElasticLegacyWriterOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task WriteLegacyEventsAsync(
        IReadOnlyList<NormalizedLegacyEvent> events,
        CancellationToken cancellationToken = default)
    {
        if (events.Count == 0) return;

        try
        {
            const string indexName = "nextraceone-obs-mf-operational-events";
            var actionLine = JsonSerializer.Serialize(new { index = new { _index = indexName } }, JsonOptions);

            var sb = new StringBuilder();
            foreach (var e in events)
            {
                sb.AppendLine(actionLine);

                var doc = new ElasticDocument(
                    Timestamp: e.Timestamp,
                    EventId: e.EventId,
                    EventType: e.EventType,
                    SourceType: e.SourceType,
                    SystemName: e.SystemName ?? "",
                    LparName: e.LparName ?? "",
                    ServiceName: e.ServiceName ?? "",
                    AssetName: e.AssetName ?? "",
                    Severity: e.Severity,
                    Message: e.Message ?? "",
                    TenantId: _options.DefaultTenantId ?? "default",
                    Attributes: e.Attributes);

                sb.AppendLine(JsonSerializer.Serialize(doc, JsonOptions));
            }

            var endpoint = _options.Endpoint.TrimEnd('/');
            var url = $"{endpoint}/_bulk";

            using var content = new StringContent(sb.ToString(), Encoding.UTF8, "application/x-ndjson");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Written {Count} legacy events to Elasticsearch", events.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write {Count} legacy events to Elasticsearch", events.Count);

            if (!_options.SuppressWriteErrors)
                throw;
        }
    }

    private sealed record ElasticDocument(
        DateTimeOffset Timestamp,
        string EventId,
        string EventType,
        string SourceType,
        string SystemName,
        string LparName,
        string ServiceName,
        string AssetName,
        string Severity,
        string Message,
        string TenantId,
        Dictionary<string, string> Attributes);
}

/// <summary>
/// Configuração do writer Elasticsearch para eventos legacy.
/// </summary>
public sealed class ElasticLegacyWriterOptions
{
    public const string SectionName = "Elastic:LegacyTelemetry";

    public string Endpoint { get; set; } = "http://localhost:9200";
    public string? DefaultTenantId { get; set; }
    public bool SuppressWriteErrors { get; set; } = true;
}
