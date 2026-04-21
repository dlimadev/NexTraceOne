using System.Text.Json;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Connectors;

/// <summary>
/// Conector para directórios locais ou de rede.
/// Indexa ficheiros de texto, markdown e outros formatos configuráveis.
///
/// Config JSON esperado:
/// {
///   "basePath": "/docs/runbooks",
///   "includeExtensions": [".md", ".txt", ".rst"],
///   "recursive": true,
///   "maxFiles": 500,
///   "maxFileSizeKb": 500
/// }
/// </summary>
internal sealed class LocalDirectoryConnector(
    ILogger<LocalDirectoryConnector> logger) : IDataSourceConnector
{
    public ExternalDataSourceConnectorType ConnectorType => ExternalDataSourceConnectorType.LocalDirectory;
    public bool SupportsIndexing => true;
    public bool SupportsRuntimeSearch => false;

    public Task<IReadOnlyList<DataSourceDocument>> FetchDocumentsAsync(
        string connectorConfigJson,
        CancellationToken ct)
    {
        var config = ParseConfig(connectorConfigJson);
        if (string.IsNullOrWhiteSpace(config.BasePath))
        {
            logger.LogWarning("LocalDirectoryConnector: basePath not configured.");
            return Task.FromResult<IReadOnlyList<DataSourceDocument>>([]);
        }

        if (!Directory.Exists(config.BasePath))
        {
            logger.LogWarning("LocalDirectoryConnector: path '{Path}' does not exist.", config.BasePath);
            return Task.FromResult<IReadOnlyList<DataSourceDocument>>([]);
        }

        var extensions = config.IncludeExtensions.Count > 0
            ? config.IncludeExtensions
            : [".md", ".txt", ".rst"];
        var maxBytes = Math.Max(1024, config.MaxFileSizeKb * 1024);
        var searchOption = config.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        var files = Directory
            .EnumerateFiles(config.BasePath, "*.*", searchOption)
            .Where(f => extensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            .Take(config.MaxFiles)
            .ToList();

        var documents = new List<DataSourceDocument>();
        foreach (var filePath in files)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var info = new FileInfo(filePath);
                if (info.Length > maxBytes) continue;

                var content = File.ReadAllText(filePath);
                var relativePath = Path.GetRelativePath(config.BasePath, filePath);

                documents.Add(new DataSourceDocument(
                    Title: relativePath,
                    Content: content,
                    SourceUrl: filePath,
                    Category: "local_directory",
                    PublishedAt: info.LastWriteTimeUtc));
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "LocalDirectoryConnector: skipping file '{Path}'.", filePath);
            }
        }

        return Task.FromResult<IReadOnlyList<DataSourceDocument>>(documents);
    }

    public Task<IReadOnlyList<DataSourceDocument>> SearchAsync(
        string connectorConfigJson,
        string query,
        int maxResults,
        CancellationToken ct)
        => Task.FromResult<IReadOnlyList<DataSourceDocument>>([]);

    private static DirectoryConfig ParseConfig(string json)
    {
        try { return JsonSerializer.Deserialize<DirectoryConfig>(json, JsonSerializerOptions.Default) ?? new DirectoryConfig(); }
        catch { return new DirectoryConfig(); }
    }

    private sealed class DirectoryConfig
    {
        public string? BasePath { get; set; }
        public List<string> IncludeExtensions { get; set; } = [];
        public bool Recursive { get; set; } = true;
        public int MaxFiles { get; set; } = 500;
        public int MaxFileSizeKb { get; set; } = 500;
    }
}
