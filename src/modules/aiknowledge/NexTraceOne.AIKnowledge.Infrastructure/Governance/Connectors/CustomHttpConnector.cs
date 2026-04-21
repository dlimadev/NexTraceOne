using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Connectors;

/// <summary>
/// Conector HTTP customizado para endpoints REST genéricos.
/// Suporta tanto indexação (fetchEndpoint) como busca em tempo real (searchEndpoint).
/// O template {query} na searchEndpoint é substituído pela query do utilizador.
///
/// Config JSON esperado:
/// {
///   "baseUrl": "https://api.example.com",
///   "authHeader": "X-API-Key",
///   "authValue": "secret",
///   "fetchEndpoint": "/docs/list",
///   "searchEndpoint": "/search?q={query}&limit={limit}",
///   "titleJsonPath": "title",
///   "contentJsonPath": "content",
///   "urlJsonPath": "url"
/// }
/// </summary>
internal sealed class CustomHttpConnector(
    IHttpClientFactory httpClientFactory,
    ILogger<CustomHttpConnector> logger) : IDataSourceConnector
{
    public ExternalDataSourceConnectorType ConnectorType => ExternalDataSourceConnectorType.CustomHttp;

    public bool SupportsIndexing => true;
    public bool SupportsRuntimeSearch => true;

    public async Task<IReadOnlyList<DataSourceDocument>> FetchDocumentsAsync(
        string connectorConfigJson,
        CancellationToken ct)
    {
        var config = ParseConfig(connectorConfigJson);
        if (string.IsNullOrWhiteSpace(config.FetchEndpoint))
            return [];

        try
        {
            var client = BuildClient(config);
            var url = $"{config.BaseUrl?.TrimEnd('/')}{config.FetchEndpoint}";
            var json = await client.GetStringAsync(url, ct);
            return ParseDocuments(json, config, "custom_http_fetch");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "CustomHttpConnector: fetch failed.");
            return [];
        }
    }

    public async Task<IReadOnlyList<DataSourceDocument>> SearchAsync(
        string connectorConfigJson,
        string query,
        int maxResults,
        CancellationToken ct)
    {
        var config = ParseConfig(connectorConfigJson);
        if (string.IsNullOrWhiteSpace(config.SearchEndpoint))
            return [];

        try
        {
            var client = BuildClient(config);
            var endpoint = config.SearchEndpoint
                .Replace("{query}", Uri.EscapeDataString(query))
                .Replace("{limit}", maxResults.ToString());
            var url = $"{config.BaseUrl?.TrimEnd('/')}{endpoint}";
            var json = await client.GetStringAsync(url, ct);
            return ParseDocuments(json, config, "custom_http_search");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "CustomHttpConnector: search failed for query '{Query}'.", query);
            return [];
        }
    }

    private static IReadOnlyList<DataSourceDocument> ParseDocuments(
        string json,
        CustomHttpConfig config,
        string category)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Support both array root and { items: [...] } / { results: [...] }
            var array = root.ValueKind == JsonValueKind.Array
                ? root
                : root.TryGetProperty("items", out var items) ? items
                : root.TryGetProperty("results", out var results) ? results
                : root.TryGetProperty("data", out var data) ? data
                : root;

            if (array.ValueKind != JsonValueKind.Array)
                return [];

            var titlePath = config.TitleJsonPath ?? "title";
            var contentPath = config.ContentJsonPath ?? "content";
            var urlPath = config.UrlJsonPath ?? "url";

            return array.EnumerateArray()
                .Select(item => new DataSourceDocument(
                    Title: item.TryGetProperty(titlePath, out var t) ? t.GetString() ?? string.Empty : string.Empty,
                    Content: item.TryGetProperty(contentPath, out var c) ? c.GetString() ?? string.Empty : string.Empty,
                    SourceUrl: item.TryGetProperty(urlPath, out var u) ? u.GetString() ?? string.Empty : string.Empty,
                    Category: category))
                .Where(d => !string.IsNullOrWhiteSpace(d.Title) && !string.IsNullOrWhiteSpace(d.Content))
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static HttpClient BuildClient(CustomHttpConfig config)
    {
        var client = new HttpClient();
        if (!string.IsNullOrWhiteSpace(config.AuthHeader) && !string.IsNullOrWhiteSpace(config.AuthValue))
            client.DefaultRequestHeaders.Add(config.AuthHeader, config.AuthValue);
        return client;
    }

    private static CustomHttpConfig ParseConfig(string json)
    {
        try { return JsonSerializer.Deserialize<CustomHttpConfig>(json, JsonOptions) ?? new CustomHttpConfig(); }
        catch { return new CustomHttpConfig(); }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class CustomHttpConfig
    {
        public string? BaseUrl { get; set; }
        public string? AuthHeader { get; set; }
        public string? AuthValue { get; set; }
        public string? FetchEndpoint { get; set; }
        public string? SearchEndpoint { get; set; }
        public string? TitleJsonPath { get; set; }
        public string? ContentJsonPath { get; set; }
        public string? UrlJsonPath { get; set; }
    }
}
