using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Connectors;

/// <summary>
/// Conector para Brave Search API.
/// Suporta busca em tempo real (SupportsRuntimeSearch = true).
/// Documentação: https://api.search.brave.com/app/documentation/web-search
///
/// Config JSON esperado:
/// {
///   "apiKey": "BSA...",
///   "countryCode": "us",
///   "searchLanguage": "en",
///   "safeSearch": "moderate"
/// }
/// </summary>
internal sealed class BraveSearchConnector(
    IHttpClientFactory httpClientFactory,
    ILogger<BraveSearchConnector> logger) : IDataSourceConnector
{
    private const string ClientName = "BraveSearch";
    private const string BaseUrl = "https://api.search.brave.com/res/v1/web/search";

    public ExternalDataSourceConnectorType ConnectorType => ExternalDataSourceConnectorType.WebSearch;
    public bool SupportsIndexing => false;
    public bool SupportsRuntimeSearch => true;

    public Task<IReadOnlyList<DataSourceDocument>> FetchDocumentsAsync(
        string connectorConfigJson,
        CancellationToken ct)
        => Task.FromResult<IReadOnlyList<DataSourceDocument>>([]);

    public async Task<IReadOnlyList<DataSourceDocument>> SearchAsync(
        string connectorConfigJson,
        string query,
        int maxResults,
        CancellationToken ct)
    {
        var config = ParseConfig(connectorConfigJson);
        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            logger.LogWarning("BraveSearchConnector: apiKey not configured.");
            return [];
        }

        try
        {
            var client = httpClientFactory.CreateClient(ClientName);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("X-Subscription-Token", config.ApiKey);

            var count = Math.Min(maxResults, 10);
            var url = $"{BaseUrl}?q={Uri.EscapeDataString(query)}&count={count}"
                + $"&country={config.CountryCode ?? "us"}"
                + $"&search_lang={config.SearchLanguage ?? "en"}"
                + $"&safesearch={config.SafeSearch ?? "moderate"}";

            var response = await client.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<BraveSearchResponse>(json, JsonOptions);

            if (result?.Web?.Results is null or { Count: 0 })
                return [];

            return result.Web.Results
                .Where(r => !string.IsNullOrWhiteSpace(r.Title) && !string.IsNullOrWhiteSpace(r.Description))
                .Select(r => new DataSourceDocument(
                    Title: r.Title!,
                    Content: r.Description!,
                    SourceUrl: r.Url ?? string.Empty,
                    Category: "web_search"))
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "BraveSearchConnector: search failed for query '{Query}'.", query);
            return [];
        }
    }

    private static BraveConfig ParseConfig(string json)
    {
        try { return JsonSerializer.Deserialize<BraveConfig>(json, JsonOptions) ?? new BraveConfig(); }
        catch { return new BraveConfig(); }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private sealed record BraveConfig(
        string? ApiKey = null,
        string? CountryCode = null,
        string? SearchLanguage = null,
        string? SafeSearch = null);

    private sealed class BraveSearchResponse
    {
        [JsonPropertyName("web")] public BraveWebResults? Web { get; set; }
    }

    private sealed class BraveWebResults
    {
        [JsonPropertyName("results")] public List<BraveResult>? Results { get; set; }
    }

    private sealed class BraveResult
    {
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
    }
}
