using System.Diagnostics;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Tools;

/// <summary>
/// Tool: pesquisa documentos na Knowledge Base por query semântica/textual.
/// Usa IKnowledgeDocumentGroundingReader para busca full-text no repositório de conhecimento.
/// </summary>
public sealed class GetKnowledgeDocsTool : IAgentTool
{
    private readonly IKnowledgeDocumentGroundingReader _knowledgeReader;
    private readonly ILogger<GetKnowledgeDocsTool> _logger;

    public GetKnowledgeDocsTool(
        IKnowledgeDocumentGroundingReader knowledgeReader,
        ILogger<GetKnowledgeDocsTool> logger)
    {
        _knowledgeReader = knowledgeReader;
        _logger = logger;
    }

    public ToolDefinition Definition => new(
        "get_knowledge_docs",
        "Searches the knowledge base for documents, runbooks, ADRs, and guides matching a query.",
        "knowledge",
        [
            new ToolParameterDefinition("query", "Search query text", "string", required: true),
            new ToolParameterDefinition("limit", "Maximum results to return (default: 5)", "integer"),
        ]);

    public async Task<ToolExecutionResult> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var (query, limit) = ParseArgs(argumentsJson);

            _logger.LogInformation("GetKnowledgeDocsTool: query={Query}, limit={Limit}", query, limit);

            var docs = await _knowledgeReader.SearchDocumentsAsync(query, limit, cancellationToken);

            var result = new
            {
                tool = "get_knowledge_docs",
                query,
                total = docs.Count,
                documents = docs.Select(d => new
                {
                    documentId = d.DocumentId,
                    title = d.Title,
                    summary = d.Summary,
                    category = d.Category,
                }),
            };

            sw.Stop();
            return new ToolExecutionResult(
                true, "get_knowledge_docs",
                JsonSerializer.Serialize(result),
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "GetKnowledgeDocsTool failed");
            return new ToolExecutionResult(false, "get_knowledge_docs", "{}", sw.ElapsedMilliseconds, ex.Message);
        }
    }

    private static (string query, int limit) ParseArgs(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return ("", 5);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var query = root.TryGetProperty("query", out var q) ? q.GetString() ?? "" : "";
            var limit = root.TryGetProperty("limit", out var l) && l.TryGetInt32(out var v) ? v : 5;
            return (query, Math.Clamp(limit, 1, 20));
        }
        catch { return ("", 5); }
    }
}
