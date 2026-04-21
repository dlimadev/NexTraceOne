using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Connectors;

/// <summary>
/// Conector para repositórios GitHub via GitHub REST API.
/// Suporta indexação em batch (SupportsIndexing = true).
/// Percorre trees de repositórios filtrando por extensão de ficheiro.
///
/// Config JSON esperado:
/// {
///   "accessToken": "ghp_...",
///   "repositories": ["owner/repo1", "owner/repo2"],
///   "includeExtensions": [".md", ".txt", ".rst"],
///   "branch": "main",
///   "maxFilesPerRepo": 200
/// }
/// </summary>
internal sealed class GitHubConnector(
    IHttpClientFactory httpClientFactory,
    ILogger<GitHubConnector> logger) : IDataSourceConnector
{
    private const string ClientName = "GitHubConnector";
    private const string ApiBase = "https://api.github.com";
    private const int MaxContentBytes = 50_000;

    public ExternalDataSourceConnectorType ConnectorType => ExternalDataSourceConnectorType.GitHub;
    public bool SupportsIndexing => true;
    public bool SupportsRuntimeSearch => false;

    public async Task<IReadOnlyList<DataSourceDocument>> FetchDocumentsAsync(
        string connectorConfigJson,
        CancellationToken ct)
    {
        var config = ParseConfig(connectorConfigJson);
        if (string.IsNullOrWhiteSpace(config.AccessToken) || config.Repositories.Count == 0)
        {
            logger.LogWarning("GitHubConnector: accessToken or repositories not configured.");
            return [];
        }

        var client = BuildClient(config.AccessToken);
        var documents = new List<DataSourceDocument>();
        var extensions = config.IncludeExtensions.Count > 0
            ? config.IncludeExtensions
            : [".md", ".txt", ".rst"];
        var maxFiles = Math.Max(1, config.MaxFilesPerRepo);

        foreach (var repo in config.Repositories.Take(10))
        {
            try
            {
                var branch = config.Branch ?? "main";
                var files = await GetRepoFilesAsync(client, repo, branch, extensions, maxFiles, ct);
                documents.AddRange(files);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "GitHubConnector: failed to index repo '{Repo}'.", repo);
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

    private async Task<List<DataSourceDocument>> GetRepoFilesAsync(
        HttpClient client,
        string repo,
        string branch,
        IReadOnlyList<string> extensions,
        int maxFiles,
        CancellationToken ct)
    {
        var treeUrl = $"{ApiBase}/repos/{repo}/git/trees/{branch}?recursive=1";
        var treeJson = await client.GetStringAsync(treeUrl, ct);
        var tree = JsonSerializer.Deserialize<GitHubTree>(treeJson, JsonOptions);

        var eligibleFiles = tree?.Tree?
            .Where(f => f.Type == "blob"
                && extensions.Any(ext => f.Path?.EndsWith(ext, StringComparison.OrdinalIgnoreCase) == true))
            .Take(maxFiles)
            .ToList() ?? [];

        var docs = new List<DataSourceDocument>();
        foreach (var file in eligibleFiles)
        {
            try
            {
                var raw = await client.GetStringAsync(
                    $"https://raw.githubusercontent.com/{repo}/{branch}/{file.Path}", ct);

                if (raw.Length > MaxContentBytes)
                    raw = raw[..MaxContentBytes];

                docs.Add(new DataSourceDocument(
                    Title: $"{repo}/{file.Path}",
                    Content: raw,
                    SourceUrl: $"https://github.com/{repo}/blob/{branch}/{file.Path}",
                    Category: "github"));
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "GitHubConnector: skipping file '{Path}'.", file.Path);
            }
        }

        return docs;
    }

    private static HttpClient BuildClient(string token)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("NexTraceOne/1.0");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }

    private static GitHubConfig ParseConfig(string json)
    {
        try { return JsonSerializer.Deserialize<GitHubConfig>(json, JsonOptions) ?? new GitHubConfig(); }
        catch { return new GitHubConfig(); }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class GitHubConfig
    {
        public string? AccessToken { get; set; }
        public List<string> Repositories { get; set; } = [];
        public List<string> IncludeExtensions { get; set; } = [];
        public string? Branch { get; set; }
        public int MaxFilesPerRepo { get; set; } = 200;
    }

    private sealed class GitHubTree
    {
        [JsonPropertyName("tree")] public List<GitHubTreeItem>? Tree { get; set; }
    }

    private sealed class GitHubTreeItem
    {
        [JsonPropertyName("path")] public string? Path { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
    }
}
