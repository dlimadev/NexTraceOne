using System.Diagnostics;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Tools;

/// <summary>
/// Tool real: pesquisa documentos no Knowledge Hub por termo textual.
/// Consulta o módulo de Knowledge via IKnowledgeDocumentGroundingReader e retorna
/// documentos relevantes (runbooks, notas operacionais, ADRs, guias técnicos) para
/// enriquecer o contexto do agent.
/// </summary>
public sealed class SearchKnowledgeTool : IAgentTool
{
    private readonly IKnowledgeDocumentGroundingReader _knowledgeReader;
    private readonly ILogger<SearchKnowledgeTool> _logger;

    public SearchKnowledgeTool(
        IKnowledgeDocumentGroundingReader knowledgeReader,
        ILogger<SearchKnowledgeTool> logger)
    {
        _knowledgeReader = knowledgeReader;
        _logger = logger;
    }

    public ToolDefinition Definition => new(
        "search_knowledge",
        "Searches the Knowledge Hub for documents, runbooks, operational notes, ADRs, and technical guides relevant to a query.",
        "knowledge",
        [
            new ToolParameterDefinition("query", "Search term or phrase (required)", "string", Required: true),
            new ToolParameterDefinition("category", "Optional category filter (e.g. Runbook, Architecture, OperationalNote, Guide)", "string"),
            new ToolParameterDefinition("limit", "Maximum number of results (default: 5, max: 20)", "integer"),
        ]);

    public async Task<ToolExecutionResult> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var args = ParseArguments(argumentsJson);

            if (string.IsNullOrWhiteSpace(args.Query))
            {
                sw.Stop();
                return new ToolExecutionResult(
                    false, "search_knowledge", string.Empty, sw.ElapsedMilliseconds,
                    "Parameter 'query' is required.");
            }

            var limit = Math.Clamp(args.Limit, 1, 20);

            _logger.LogInformation(
                "SearchKnowledgeTool executing for query={Query}, category={Category}, limit={Limit}",
                args.Query, args.Category, limit);

            var documents = await _knowledgeReader.SearchDocumentsAsync(
                searchTerm: args.Query,
                maxResults: limit * 3, // Fetch more to allow category post-filter
                ct: cancellationToken);

            // Apply category filter post-retrieval
            var filtered = string.IsNullOrWhiteSpace(args.Category)
                ? documents
                : documents
                    .Where(d => string.Equals(d.Category, args.Category, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            var result = new
            {
                tool = "search_knowledge",
                status = "executed",
                query = args.Query,
                category = args.Category,
                total = filtered.Count,
                documents = filtered.Take(limit).Select(d => new
                {
                    documentId = d.DocumentId,
                    title = d.Title,
                    summary = d.Summary,
                    category = d.Category,
                }),
            };

            sw.Stop();
            var output = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false });

            return new ToolExecutionResult(true, "search_knowledge", output, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "SearchKnowledgeTool failed");
            return new ToolExecutionResult(
                false, "search_knowledge", string.Empty, sw.ElapsedMilliseconds, ex.Message);
        }
    }

    private static SearchKnowledgeArgs ParseArguments(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new SearchKnowledgeArgs(null, null, 5);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var query = root.TryGetProperty("query", out var qProp) ? qProp.GetString() : null;
            var category = root.TryGetProperty("category", out var catProp) ? catProp.GetString() : null;
            var limit = root.TryGetProperty("limit", out var limProp) && limProp.TryGetInt32(out var l) ? l : 5;

            return new SearchKnowledgeArgs(query, category, limit);
        }
        catch
        {
            return new SearchKnowledgeArgs(null, null, 5);
        }
    }

    private sealed record SearchKnowledgeArgs(string? Query, string? Category, int Limit);
}
