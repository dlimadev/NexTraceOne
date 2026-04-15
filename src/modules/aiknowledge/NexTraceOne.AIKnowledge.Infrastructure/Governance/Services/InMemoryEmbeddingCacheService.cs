using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Services;

/// <summary>
/// Cache de embeddings em memória com limite de entradas e evicção LRU via IMemoryCache.
/// Evita recalculações durante a sessão para textos frequentemente consultados.
/// Thread-safe via IMemoryCache interna. (E-M03)
/// </summary>
public sealed class InMemoryEmbeddingCacheService : IEmbeddingCacheService, IDisposable
{
    private const int MaxCacheEntries = 500;
    private const string DefaultEmbeddingModel = "nomic-embed-text";

    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly ILogger<InMemoryEmbeddingCacheService> _logger;
    private readonly MemoryCache _cache;

    private int _hitCount;
    private int _missCount;
    private int _evictionCount;

    public InMemoryEmbeddingCacheService(
        IEmbeddingProvider embeddingProvider,
        ILogger<InMemoryEmbeddingCacheService> logger)
    {
        _embeddingProvider = embeddingProvider;
        _logger = logger;
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = MaxCacheEntries
        });
    }

    /// <summary>Número de acertos no cache desde o início da instância.</summary>
    public int HitCount => _hitCount;

    /// <summary>Número de faltas no cache desde o início da instância.</summary>
    public int MissCount => _missCount;

    /// <summary>Número de entradas eviccionadas desde o início da instância.</summary>
    public int EvictionCount => _evictionCount;

    public async Task<float[]> GetOrComputeAsync(string text, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        if (_cache.TryGetValue(text, out float[]? cached) && cached is not null)
        {
            Interlocked.Increment(ref _hitCount);
            return cached;
        }

        Interlocked.Increment(ref _missCount);
        _logger.LogDebug("Computing embedding for text of length {Length}", text.Length);

        var result = await _embeddingProvider.GenerateEmbeddingsAsync(
            new EmbeddingRequest(DefaultEmbeddingModel, [text]),
            ct);

        if (!result.Success || result.Embeddings is null || result.Embeddings.Count == 0)
        {
            _logger.LogWarning(
                "Embedding computation failed: {Error}", result.ErrorMessage ?? "unknown");
            return [];
        }

        var embedding = result.Embeddings[0];

        var entryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(30))
            .SetSize(1)
            .RegisterPostEvictionCallback((_, _, reason, _) =>
            {
                if (reason != EvictionReason.Replaced)
                    Interlocked.Increment(ref _evictionCount);
            });

        _cache.Set(text, embedding, entryOptions);
        return embedding;
    }

    public void Dispose() => _cache.Dispose();
}
