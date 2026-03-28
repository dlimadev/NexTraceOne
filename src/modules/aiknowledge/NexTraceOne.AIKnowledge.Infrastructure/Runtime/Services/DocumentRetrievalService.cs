using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

/// <summary>
/// Implementação real do serviço de retrieval de documentos para grounding de IA.
/// Combina duas fontes de conhecimento:
///   1. KnowledgeDocuments do Knowledge Hub (runbooks, docs operacionais, post-mortems)
///      via IKnowledgeDocumentGroundingReader (somente-leitura cross-módulo)
///   2. AIKnowledgeSources registadas no módulo AIKnowledge (configurações, endpoints)
/// Retorna resultados relevantes. Falha silenciosamente por fonte — nunca bloqueia o pipeline.
/// </summary>
public sealed class DocumentRetrievalService : IDocumentRetrievalService
{
    private const int MaxSnippetLength = 250;

    private readonly IAiKnowledgeSourceRepository _sourceRepo;
    private readonly IKnowledgeDocumentGroundingReader _knowledgeDocReader;
    private readonly ILogger<DocumentRetrievalService> _logger;

    public DocumentRetrievalService(
        IAiKnowledgeSourceRepository sourceRepo,
        IKnowledgeDocumentGroundingReader knowledgeDocReader,
        ILogger<DocumentRetrievalService> logger)
    {
        _sourceRepo = sourceRepo;
        _knowledgeDocReader = knowledgeDocReader;
        _logger = logger;
    }

    public async Task<DocumentSearchResult> SearchAsync(
        DocumentSearchRequest request,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Document retrieval requested for query '{Query}' with max {MaxResults} results",
            request.Query, request.MaxResults);

        var hits = new List<DocumentSearchHit>();

        // ── 1. Knowledge Hub documents (runbooks, operational docs, post-mortems) ──
        try
        {
            var docs = await _knowledgeDocReader.SearchDocumentsAsync(
                request.Query, request.MaxResults, ct);

            foreach (var doc in docs)
            {
                hits.Add(new DocumentSearchHit(
                    SourceId: "KnowledgeHub",
                    DocumentId: doc.DocumentId,
                    Title: doc.Title,
                    Snippet: doc.Summary ?? string.Empty,
                    RelevanceScore: 0.90,
                    Classification: doc.Category));
            }

            _logger.LogDebug(
                "Knowledge Hub grounding: {Count} documents retrieved for query='{Query}'",
                docs.Count, request.Query);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Knowledge document retrieval failed for query='{Query}' — continuing without",
                request.Query);
        }

        // ── 2. AI Knowledge Sources (registered source endpoints) ──
        try
        {
            var sources = await _sourceRepo.ListAsync(
                sourceType: null,
                isActive: true,
                ct: ct);

            var query = request.Query;
            var sourceHits = sources
                .Where(s =>
                    s.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    s.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    s.EndpointOrPath.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Where(s => string.IsNullOrWhiteSpace(request.SourceFilter) ||
                    s.SourceType.ToString().Equals(request.SourceFilter, StringComparison.OrdinalIgnoreCase))
                .Take(request.MaxResults)
                .Select((s, index) => new DocumentSearchHit(
                    SourceId: s.SourceType.ToString(),
                    DocumentId: s.Id.Value.ToString(),
                    Title: s.Name,
                    Snippet: Truncate(s.Description, MaxSnippetLength),
                    RelevanceScore: Math.Max(0.0, 0.75 - (index * 0.1)),
                    Classification: s.SourceType.ToString()))
                .ToList();

            hits.AddRange(sourceHits);

            _logger.LogDebug(
                "AI knowledge source grounding: {Count} sources matched query='{Query}'",
                sourceHits.Count, query);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "AI knowledge source retrieval failed for query='{Query}' — continuing without",
                request.Query);
        }

        _logger.LogDebug(
            "Document retrieval found {HitCount} results for query '{Query}'",
            hits.Count, request.Query);

        return new DocumentSearchResult(Success: true, Hits: hits.Take(request.MaxResults).ToList());
    }

    private static string Truncate(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        return text.Length <= maxLength ? text : string.Concat(text.AsSpan(0, maxLength - 3), "...");
    }
}
