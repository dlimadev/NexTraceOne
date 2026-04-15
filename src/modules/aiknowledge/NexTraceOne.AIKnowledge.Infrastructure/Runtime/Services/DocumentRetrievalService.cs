using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

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

        // ── 2. AI Knowledge Sources — pgvector ANN search with in-memory fallback ──
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

            var queryEmbedding = await TryGetQueryEmbeddingAsync(request.Query, ct);

            if (queryEmbedding.Length > 0)
            {
                // ── Tentativa 1: pgvector ANN via base de dados (E-A01) ──
                var pgvectorHits = await TryPgVectorSearchAsync(
                    queryEmbedding, request.MaxResults, sourcesFiltered, ct);

                if (pgvectorHits.Count > 0)
                {
                    hits.AddRange(pgvectorHits);
                    _logger.LogDebug(
                        "AI knowledge source grounding (pgvector ANN): {Count} matches for query='{Query}'",
                        pgvectorHits.Count, request.Query);
                }
                else
                {
                    // ── Tentativa 2: cosine em memória (fallback) ──
                    var cosineHits = ComputeInMemoryCosineHits(
                        queryEmbedding, sourcesFiltered, request.MaxResults);
                    hits.AddRange(cosineHits);

                    if (cosineHits.Count > 0)
                    {
                        _logger.LogDebug(
                            "AI knowledge source grounding (in-memory cosine): {Count} matches for query='{Query}'",
                            cosineHits.Count, request.Query);
                    }
                    else
                    {
                        AddStringMatchHits(hits, sourcesFiltered, request);
                    }
                }
            }
            else
            {
                // Sem embedding disponível — fallback a string search
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

    private async Task<float[]> TryGetQueryEmbeddingAsync(string query, CancellationToken ct)
    {
        try
        {
            return await _embeddingCache.GetOrComputeAsync(query, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to compute query embedding for '{Query}' — falling back to string search",
                query);
            return [];
        }
    }

    /// <summary>
    /// Tenta busca semântica ANN via pgvector. Retorna lista vazia se pgvector
    /// não estiver disponível ou se não houver vectores indexados. (E-A01)
    /// </summary>
    private async Task<List<DocumentSearchHit>> TryPgVectorSearchAsync(
        float[] queryEmbedding,
        int maxResults,
        List<AIKnowledgeSource> allSources,
        CancellationToken ct)
    {
        try
        {
            var pgResults = await _sourceRepo.SearchByVectorAsync(queryEmbedding, maxResults, ct);

            if (pgResults.Count == 0)
                return [];

            // Mapear IDs para entidades para obter título e snippet
            var sourceById = allSources.ToDictionary(s => s.Id.Value);
            var hits = new List<DocumentSearchHit>(pgResults.Count);

            foreach (var (id, score) in pgResults)
            {
                if (!sourceById.TryGetValue(id.Value, out var source))
                    continue;
                if (score < 0.1)
                    continue;

                hits.Add(new DocumentSearchHit(
                    SourceId: source.SourceType.ToString(),
                    DocumentId: source.Id.Value.ToString(),
                    Title: source.Name,
                    Snippet: Truncate(source.Description, MaxSnippetLength),
                    RelevanceScore: score,
                    Classification: source.SourceType.ToString()));
            }

            return hits;
        }
        catch
        {
            return [];
        }
    }

    private List<DocumentSearchHit> ComputeInMemoryCosineHits(
        float[] queryEmbedding,
        List<AIKnowledgeSource> sources,
        int maxResults)
    {
        return sources
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
            .Take(maxResults)
            .Select(x => new DocumentSearchHit(
                SourceId: x.Source.SourceType.ToString(),
                DocumentId: x.Source.Id.Value.ToString(),
                Title: x.Source.Name,
                Snippet: Truncate(x.Source.Description, MaxSnippetLength),
                RelevanceScore: x.Score,
                Classification: x.Source.SourceType.ToString()))
            .ToList();
    }

    private static void AddStringMatchHits(
        List<DocumentSearchHit> hits,
        List<AIKnowledgeSource> sources,
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
