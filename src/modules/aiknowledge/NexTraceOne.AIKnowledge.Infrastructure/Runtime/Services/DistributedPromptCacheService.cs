using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

/// <summary>
/// Implementação distribuída do cache de prompts usando IDistributedCache (Redis ou in-memory).
/// TTL padrão: 5 minutos para respostas de chat, 1 hora para embeddings.
/// </summary>
public sealed class DistributedPromptCacheService : IPromptCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedPromptCacheService> _logger;
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    public DistributedPromptCacheService(
        IDistributedCache cache,
        ILogger<DistributedPromptCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<string?> GetCachedResponseAsync(string promptHash, string modelId, CancellationToken ct = default)
    {
        var key = BuildCacheKey(promptHash, modelId);
        try
        {
            var cached = await _cache.GetStringAsync(key, ct);
            if (!string.IsNullOrEmpty(cached))
            {
                _logger.LogDebug("Prompt cache HIT for key={Key}", key);
                return cached;
            }
            _logger.LogDebug("Prompt cache MISS for key={Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Prompt cache read failed for key={Key} — continuing without cache", key);
            return null;
        }
    }

    public async Task CacheResponseAsync(string promptHash, string modelId, string response, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        var key = BuildCacheKey(promptHash, modelId);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl ?? DefaultTtl
        };

        try
        {
            await _cache.SetStringAsync(key, response, options, ct);
            _logger.LogDebug("Prompt response cached for key={Key}, ttl={Ttl}s", key, options.AbsoluteExpirationRelativeToNow?.TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Prompt cache write failed for key={Key} — continuing", key);
        }
    }

    public string ComputePromptHash(string prompt, string modelId, double? temperature = null, int? maxTokens = null)
    {
        // Normalização: trim + lowercase para hits em variações triviais de whitespace/case
        var normalized = prompt.Trim().ToLowerInvariant();
        var input = $"{modelId}:{temperature}:{maxTokens}:{normalized}";

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(hash);
    }

    public async Task InvalidateAsync(string prefix, CancellationToken ct = default)
    {
        // Nota: IDistributedCache não suporta invalidação por prefixo nativamente.
        // Em produção com Redis, usar StackExchange.Redis diretamente para SCAN + DEL.
        // Aqui logamos a necessidade de invalidação; invalidação real requer implementação Redis-specific.
        _logger.LogInformation("Prompt cache invalidation requested for prefix={Prefix}. " +
            "Note: prefix-based invalidation requires direct Redis access (not supported by IDistributedCache)", prefix);

        await Task.CompletedTask;
    }

    private static string BuildCacheKey(string promptHash, string modelId)
    {
        // Namespace de cache para evitar colisões com outras funcionalidades
        return $"ai:prompt:v1:{modelId}:{promptHash}";
    }
}
