using System.Diagnostics;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Tools;

/// <summary>
/// Tool real: pesquisa runbooks operacionais relevantes no Knowledge Hub.
/// Especialização do SearchKnowledge focada em documentos de categoria Runbook,
/// útil para agents de investigação operacional, mitigação de incidentes e on-call support.
/// </summary>
public sealed class GetRunbookTool : IAgentTool
{
    private readonly IKnowledgeDocumentGroundingReader _knowledgeReader;
    private readonly ILogger<GetRunbookTool> _logger;

    public GetRunbookTool(
        IKnowledgeDocumentGroundingReader knowledgeReader,
        ILogger<GetRunbookTool> logger)
    {
        _knowledgeReader = knowledgeReader;
        _logger = logger;
    }

    public ToolDefinition Definition => new(
        "get_runbook",
        "Retrieves operational runbooks from the Knowledge Hub. Useful for incident investigation, mitigation steps, and on-call support.",
        "knowledge",
        [
            new ToolParameterDefinition("serviceName", "Service name or identifier to find runbooks for", "string"),
            new ToolParameterDefinition("keywords", "Additional search keywords (e.g. 'database failover', 'rollback', 'restart')", "string"),
            new ToolParameterDefinition("limit", "Maximum number of runbooks (default: 5)", "integer"),
        ]);

    public async Task<ToolExecutionResult> ExecuteAsync(
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var args = ParseArguments(argumentsJson);
            var limit = Math.Clamp(args.Limit, 1, 20);

            // Build a composite search term: service name + keywords + "runbook"
            var searchParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(args.ServiceName))
                searchParts.Add(args.ServiceName);
            if (!string.IsNullOrWhiteSpace(args.Keywords))
                searchParts.Add(args.Keywords);
            searchParts.Add("runbook");

            var searchTerm = string.Join(" ", searchParts);

            _logger.LogInformation(
                "GetRunbookTool executing for serviceName={ServiceName}, keywords={Keywords}, limit={Limit}",
                args.ServiceName, args.Keywords, limit);

            var documents = await _knowledgeReader.SearchDocumentsAsync(
                searchTerm: searchTerm,
                maxResults: limit * 3,
                ct: cancellationToken);

            // Prefer documents with "Runbook" in the category
            var runbooks = documents
                .OrderByDescending(d =>
                    string.Equals(d.Category, "Runbook", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                .Take(limit)
                .ToList();

            var result = new
            {
                tool = "get_runbook",
                status = "executed",
                serviceName = args.ServiceName,
                keywords = args.Keywords,
                total = runbooks.Count,
                runbooks = runbooks.Select(d => new
                {
                    documentId = d.DocumentId,
                    title = d.Title,
                    summary = d.Summary,
                    category = d.Category,
                }),
                guidance = runbooks.Count == 0
                    ? "No runbooks found. Consider creating one in the Knowledge Hub for this service."
                    : null,
            };

            sw.Stop();
            var output = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false });

            return new ToolExecutionResult(true, "get_runbook", output, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "GetRunbookTool failed");
            return new ToolExecutionResult(
                false, "get_runbook", string.Empty, sw.ElapsedMilliseconds, ex.Message);
        }
    }

    private static GetRunbookArgs ParseArguments(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new GetRunbookArgs(null, null, 5);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var service = root.TryGetProperty("serviceName", out var svcProp) ? svcProp.GetString() : null;
            var keywords = root.TryGetProperty("keywords", out var kwProp) ? kwProp.GetString() : null;
            var limit = root.TryGetProperty("limit", out var limProp) && limProp.TryGetInt32(out var l) ? l : 5;

            return new GetRunbookArgs(service, keywords, limit);
        }
        catch
        {
            return new GetRunbookArgs(null, null, 5);
        }
    }

    private sealed record GetRunbookArgs(string? ServiceName, string? Keywords, int Limit);
}
