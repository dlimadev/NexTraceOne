using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.VectorStore;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

/// <summary>
/// Implementação de IRagGroundingService usando embeddings + Qdrant vector search.
/// Fail-open: se embedding ou vector search falhar, retorna null em vez de quebrar o pipeline.
/// </summary>
public sealed class RagGroundingService : IRagGroundingService
{
    private readonly IEnumerable<IEmbeddingProvider> _embeddingProviders;
    private readonly IVectorStoreRepository _vectorStore;
    private readonly ILogger<RagGroundingService> _logger;

    // nomic-embed-text (Ollama default) produces 768-dim vectors.
    // OpenAI text-embedding-3-small produces 1536-dim vectors.
    // We default to 768 and adjust dynamically on first use per collection.
    private readonly Dictionary<string, int> _collectionVectorSizes = new(StringComparer.Ordinal);

    public RagGroundingService(
        IEnumerable<IEmbeddingProvider> embeddingProviders,
        IVectorStoreRepository vectorStore,
        ILogger<RagGroundingService> logger)
    {
        _embeddingProviders = embeddingProviders;
        _vectorStore = vectorStore;
        _logger = logger;
    }

    public async Task<string?> GetGroundingContextAsync(
        string query,
        string collectionName = "aiknowledge",
        int topK = 5,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogDebug("RAG grounding skipped: query is empty");
            return null;
        }

        var provider = _embeddingProviders.FirstOrDefault();
        if (provider is null)
        {
            _logger.LogWarning("RAG grounding skipped: no IEmbeddingProvider registered");
            return null;
        }

        try
        {
            // 1. Generate query embedding
            var embedRequest = new EmbeddingRequest(provider.ProviderId, new[] { query });
            var embedResult = await provider.GenerateEmbeddingsAsync(embedRequest, ct);

            if (!embedResult.Success || embedResult.Embeddings is null || embedResult.Embeddings.Count == 0)
            {
                _logger.LogWarning("RAG grounding failed: embedding generation unsuccessful for provider {Provider}", provider.ProviderId);
                return null;
            }

            var queryVector = embedResult.Embeddings[0];

            // 2. Ensure collection exists (dynamic size detection on first use)
            if (!_collectionVectorSizes.TryGetValue(collectionName, out var vectorSize))
            {
                vectorSize = queryVector.Length;
                _collectionVectorSizes[collectionName] = vectorSize;
            }

            await _vectorStore.EnsureCollectionAsync(collectionName, vectorSize, ct);

            // 3. Vector search
            var results = await _vectorStore.SearchAsync(collectionName, queryVector, topK, ct);

            if (results.Count == 0)
            {
                _logger.LogDebug("RAG grounding: no relevant vectors found in collection {Collection} for query '{Query}'", collectionName, query);
                return null;
            }

            // 4. Build grounding context string from metadata
            var contextParts = new List<string>(results.Count);
            foreach (var result in results)
            {
                var content = result.Metadata.GetValueOrDefault("content")?.ToString()
                    ?? result.Metadata.GetValueOrDefault("snippet")?.ToString()
                    ?? result.Metadata.GetValueOrDefault("description")?.ToString()
                    ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(content))
                {
                    contextParts.Add($"- {content.Trim()}");
                }
            }

            if (contextParts.Count == 0)
            {
                return null;
            }

            var context = string.Join("\n", contextParts);
            _logger.LogInformation(
                "RAG grounding: retrieved {Count} relevant items from collection {Collection} for query '{Query}'",
                contextParts.Count, collectionName, query);

            return context;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RAG grounding failed for query '{Query}' — returning null", query);
            return null;
        }
    }
}
