using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Services;

/// <summary>
/// Cache de embeddings em memória com limite de 500 entradas.
/// Evita recalculações durante a sessão para textos frequentemente consultados.
/// Implementação simples adequada ao MVP1 — pode evoluir para cache distribuído.
/// </summary>
public sealed class InMemoryEmbeddingCacheService(
    IEmbeddingProvider embeddingProvider,
    ILogger<InMemoryEmbeddingCacheService> logger) : IEmbeddingCacheService
{
    private const int MaxCacheEntries = 500;
    private const string DefaultEmbeddingModel = "nomic-embed-text";

    private readonly ConcurrentDictionary<string, float[]> _cache = new(StringComparer.Ordinal);

    public async Task<float[]> GetOrComputeAsync(string text, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        if (_cache.TryGetValue(text, out var cached))
            return cached;

        // Evict cache when limit is reached — remove oldest-inserted entries (aproximado)
        if (_cache.Count >= MaxCacheEntries)
        {
            var keyToRemove = _cache.Keys.FirstOrDefault();
            if (keyToRemove is not null)
                _cache.TryRemove(keyToRemove, out _);
        }

        logger.LogDebug("Computing embedding for text of length {Length}", text.Length);

        var result = await embeddingProvider.GenerateEmbeddingsAsync(
            new EmbeddingRequest(DefaultEmbeddingModel, [text]),
            ct);

        if (!result.Success || result.Embeddings is null || result.Embeddings.Count == 0)
        {
            logger.LogWarning(
                "Embedding computation failed: {Error}", result.ErrorMessage ?? "unknown");
            return [];
        }

        var embedding = result.Embeddings[0];
        _cache.TryAdd(text, embedding);
        return embedding;
    }
}
