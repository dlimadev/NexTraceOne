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
/// Quando as fontes possuem EmbeddingJson, usa similaridade coseno via IEmbeddingCacheService
/// para ranking semântico. Fallback a string contains quando não há embeddings.
/// Retorna resultados relevantes. Falha silenciosamente por fonte — nunca bloqueia o pipeline.
/// </summary>
public sealed class DocumentRetrievalService : IDocumentRetrievalService
{
    private const int MaxSnippetLength = 250;

    private readonly IAiKnowledgeSourceRepository _sourceRepo;
    private readonly IKnowledgeDocumentGroundingReader _knowledgeDocReader;
    private readonly IEmbeddingCacheService _embeddingCache;
    private readonly ILogger<DocumentRetrievalService> _logger;

    public DocumentRetrievalService(
        IAiKnowledgeSourceRepository sourceRepo,
        IKnowledgeDocumentGroundingReader knowledgeDocReader,
        IEmbeddingCacheService embeddingCache,
        ILogger<DocumentRetrievalService> logger)
    {
        _sourceRepo = sourceRepo;
        _knowledgeDocReader = knowledgeDocReader;
        _embeddingCache = embeddingCache;
        _logger = logger;
    }

    /// <summary>Construtor backward-compatible sem cache de embeddings (fallback a string search).</summary>
    public DocumentRetrievalService(
        IAiKnowledgeSourceRepository sourceRepo,
        IKnowledgeDocumentGroundingReader knowledgeDocReader,
        ILogger<DocumentRetrievalService> logger)
        : this(sourceRepo, knowledgeDocReader, NullEmbeddingCacheService.Instance, logger)
    {
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

            var sourcesFiltered = sources
                .Where(s => string.IsNullOrWhiteSpace(request.SourceFilter) ||
                    s.SourceType.ToString().Equals(request.SourceFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Usar similaridade coseno quando embeddings estão disponíveis
            var hasEmbeddings = sourcesFiltered.Any(s => s.EmbeddingJson is not null);

            if (hasEmbeddings)
            {
                float[] queryEmbedding;
                try
                {
                    queryEmbedding = await _embeddingCache.GetOrComputeAsync(request.Query, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to compute query embedding — falling back to string search");
                    queryEmbedding = [];
                }

                if (queryEmbedding.Length > 0)
                {
                    var sourceHits = sourcesFiltered
                        .Select(s =>
                        {
                            var emb = s.GetEmbedding();
                            var score = emb is not null && emb.Length == queryEmbedding.Length
                                ? CosineSimilarity(queryEmbedding, emb)
                                : 0.0;
                            return (Source: s, Score: score);
                        })
                        .Where(x => x.Score > 0.1)
                        .OrderByDescending(x => x.Score)
                        .Take(request.MaxResults)
                        .Select(x => new DocumentSearchHit(
                            SourceId: x.Source.SourceType.ToString(),
                            DocumentId: x.Source.Id.Value.ToString(),
                            Title: x.Source.Name,
                            Snippet: Truncate(x.Source.Description, MaxSnippetLength),
                            RelevanceScore: x.Score,
                            Classification: x.Source.SourceType.ToString()))
                        .ToList();

                    hits.AddRange(sourceHits);

                    _logger.LogDebug(
                        "AI knowledge source grounding (semantic): {Count} sources matched query='{Query}'",
                        sourceHits.Count, request.Query);
                }
                else
                {
                    // Fallback a string search quando embedding falhou
                    AddStringMatchHits(hits, sourcesFiltered, request);
                }
            }
            else
            {
                // Fallback a string search quando fontes não têm embeddings
                AddStringMatchHits(hits, sourcesFiltered, request);
            }
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

    private static void AddStringMatchHits(
        List<DocumentSearchHit> hits,
        List<AIKnowledge.Domain.Governance.Entities.AIKnowledgeSource> sources,
        DocumentSearchRequest request)
    {
        var query = request.Query;
        var sourceHits = sources
            .Where(s =>
                s.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                s.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                s.EndpointOrPath.Contains(query, StringComparison.OrdinalIgnoreCase))
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
    }

    /// <summary>Calcula a similaridade coseno entre dois vetores de float.</summary>
    internal static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length || a.Length == 0)
            return 0.0;

        double dot = 0.0, normA = 0.0, normB = 0.0;

        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA == 0.0 || normB == 0.0)
            return 0.0;

        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    private static string Truncate(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        return text.Length <= maxLength ? text : string.Concat(text.AsSpan(0, maxLength - 3), "...");
    }
}
