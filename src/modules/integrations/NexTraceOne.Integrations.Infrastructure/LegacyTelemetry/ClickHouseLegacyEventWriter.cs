using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexTraceOne.Integrations.Application.LegacyTelemetry.Abstractions;
using NexTraceOne.Integrations.Domain.LegacyTelemetry;

namespace NexTraceOne.Integrations.Infrastructure.LegacyTelemetry;

/// <summary>
/// Writer para persistência de eventos legacy normalizados em ClickHouse.
/// Tabela alvo: nextraceone_obs.mf_operational_events
/// </summary>
public sealed class ClickHouseLegacyEventWriter : ILegacyEventWriter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClickHouseLegacyEventWriter> _logger;
    private readonly ClickHouseLegacyWriterOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ClickHouseLegacyEventWriter(
        HttpClient httpClient,
        ILogger<ClickHouseLegacyEventWriter> logger,
        IOptions<ClickHouseLegacyWriterOptions> options)
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
            var rows = events.Select(e => new ClickHouseRow(
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
                Attributes: e.Attributes
            )).ToList();

            var query = "INSERT INTO nextraceone_obs.mf_operational_events FORMAT JSONEachRow";
            var url = $"{_options.Endpoint}/?query={Uri.EscapeDataString(query)}";

            var jsonLines = string.Join("\n", rows.Select(r => JsonSerializer.Serialize(r, JsonOptions)));
            var content = new StringContent(jsonLines, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Written {Count} legacy events to ClickHouse", events.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write {Count} legacy events to ClickHouse", events.Count);

            if (!_options.SuppressWriteErrors)
                throw;
        }
    }

    private sealed record ClickHouseRow(
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
/// Configuração do writer ClickHouse para eventos legacy.
/// </summary>
public sealed class ClickHouseLegacyWriterOptions
{
    public const string SectionName = "ClickHouse:LegacyTelemetry";

    public string Endpoint { get; set; } = "http://localhost:8123";
    public string? DefaultTenantId { get; set; }
    public bool SuppressWriteErrors { get; set; } = true;
}
