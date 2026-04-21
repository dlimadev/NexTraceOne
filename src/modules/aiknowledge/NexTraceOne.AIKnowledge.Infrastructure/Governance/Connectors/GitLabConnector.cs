using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Connectors;

/// <summary>
/// Conector para repositórios GitLab via GitLab REST API v4.
/// Suporta instâncias self-hosted (baseUrl configurável).
///
/// Config JSON esperado:
/// {
///   "accessToken": "glpat-...",
///   "baseUrl": "https://gitlab.com",
///   "projectIds": [123, 456],
///   "includeExtensions": [".md", ".txt"],
///   "branch": "main",
///   "maxFilesPerProject": 200
/// }
/// </summary>
internal sealed class GitLabConnector(
    IHttpClientFactory httpClientFactory,
    ILogger<GitLabConnector> logger) : IDataSourceConnector
{
    private const int MaxContentBytes = 50_000;

    public ExternalDataSourceConnectorType ConnectorType => ExternalDataSourceConnectorType.GitLab;
    public bool SupportsIndexing => true;
    public bool SupportsRuntimeSearch => false;

    public async Task<IReadOnlyList<DataSourceDocument>> FetchDocumentsAsync(
        string connectorConfigJson,
        CancellationToken ct)
    {
        var config = ParseConfig(connectorConfigJson);
        if (string.IsNullOrWhiteSpace(config.AccessToken) || config.ProjectIds.Count == 0)
        {
            logger.LogWarning("GitLabConnector: accessToken or projectIds not configured.");
            return [];
        }

        var baseUrl = config.BaseUrl?.TrimEnd('/') ?? "https://gitlab.com";
        var client = BuildClient(config.AccessToken);
        var documents = new List<DataSourceDocument>();
        var extensions = config.IncludeExtensions.Count > 0
            ? config.IncludeExtensions
            : [".md", ".txt", ".rst"];
        var maxFiles = Math.Max(1, config.MaxFilesPerProject);

        foreach (var projectId in config.ProjectIds.Take(10))
        {
            try
            {
                var docs = await IndexProjectAsync(client, baseUrl, projectId, config.Branch ?? "main", extensions, maxFiles, ct);
                documents.AddRange(docs);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "GitLabConnector: failed to index project {ProjectId}.", projectId);
            }
        }

        return documents;
    }

    public Task<IReadOnlyList<DataSourceDocument>> SearchAsync(
        string connectorConfigJson,
        string query,
        int maxResults,
        CancellationToken ct)
        => Task.FromResult<IReadOnlyList<DataSourceDocument>>([]);

    private async Task<List<DataSourceDocument>> IndexProjectAsync(
        HttpClient client,
        string baseUrl,
        int projectId,
        string branch,
        IReadOnlyList<string> extensions,
        int maxFiles,
        CancellationToken ct)
    {
        var apiBase = $"{baseUrl}/api/v4/projects/{projectId}";
        var treeUrl = $"{apiBase}/repository/tree?recursive=true&per_page=100&ref={branch}";

        var treeJson = await client.GetStringAsync(treeUrl, ct);
        var items = JsonSerializer.Deserialize<List<GitLabTreeItem>>(treeJson, JsonOptions) ?? [];

        var eligibleFiles = items
            .Where(f => f.Type == "blob"
                && extensions.Any(ext => f.Path?.EndsWith(ext, StringComparison.OrdinalIgnoreCase) == true))
            .Take(maxFiles)
            .ToList();

        var docs = new List<DataSourceDocument>();
        foreach (var file in eligibleFiles)
        {
            try
            {
                var encodedPath = Uri.EscapeDataString(file.Path ?? string.Empty);
                var fileUrl = $"{apiBase}/repository/files/{encodedPath}/raw?ref={branch}";
                var content = await client.GetStringAsync(fileUrl, ct);

                if (content.Length > MaxContentBytes)
                    content = content[..MaxContentBytes];

                docs.Add(new DataSourceDocument(
                    Title: $"project-{projectId}/{file.Path}",
                    Content: content,
                    SourceUrl: $"{baseUrl.Replace("/api/v4", "")}/-/blob/{branch}/{file.Path}",
                    Category: "gitlab"));
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "GitLabConnector: skipping file '{Path}'.", file.Path);
            }
        }

        return docs;
    }

    private static HttpClient BuildClient(string token)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("PRIVATE-TOKEN", token);
        return client;
    }

    private static GitLabConfig ParseConfig(string json)
    {
        try { return JsonSerializer.Deserialize<GitLabConfig>(json, JsonOptions) ?? new GitLabConfig(); }
        catch { return new GitLabConfig(); }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class GitLabConfig
    {
        public string? AccessToken { get; set; }
        public string? BaseUrl { get; set; }
        public List<int> ProjectIds { get; set; } = [];
        public List<string> IncludeExtensions { get; set; } = [];
        public string? Branch { get; set; }
        public int MaxFilesPerProject { get; set; } = 200;
    }

    private sealed class GitLabTreeItem
    {
        [JsonPropertyName("path")] public string? Path { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
    }
}
