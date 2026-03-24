using Microsoft.Extensions.Caching.Memory;

using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Infrastructure.Services;

/// <summary>
/// Implementação de cache de configurações usando IMemoryCache.
/// Chaves prefixadas com "cfg:" para isolamento. Expiração padrão de 5 minutos.
/// Invalidação global via contador de versão para evitar enumerar chaves.
/// </summary>
internal sealed class ConfigurationCacheService(IMemoryCache memoryCache) : IConfigurationCacheService
{
    private const string CachePrefix = "cfg:";
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Contador de versão global para invalidação em massa.
    /// Quando incrementado, todas as chaves compostas com a versão anterior tornam-se misses.
    /// </summary>
    private long _version;

    public Task<T> GetOrSetAsync<T>(string cacheKey, Func<Task<T>> factory, CancellationToken cancellationToken)
    {
        var versionedKey = BuildVersionedKey(cacheKey);

        return memoryCache.GetOrCreateAsync(versionedKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = DefaultExpiration;
            return await factory();
        })!;
    }

    public Task InvalidateAsync(string key, ConfigurationScope? scope, CancellationToken cancellationToken)
    {
        if (scope.HasValue)
        {
            // Invalidar chaves específicas de resolução para este key+scope
            // Dado que IMemoryCache não suporta enumeração, usamos prefixo convencional
            var pattern = $"cfg:resolve:{key}:{scope.Value}:";
            memoryCache.Remove(BuildVersionedKey(pattern));
        }

        // Invalidar a chave genérica de resolução individual
        var resolveKeyPrefix = $"cfg:resolve:{key}:";

        // Invalidar resolve-all para todos os scopes na hierarquia
        foreach (var s in Enum.GetValues<ConfigurationScope>())
        {
            memoryCache.Remove(BuildVersionedKey($"cfg:resolve-all:{s}:null"));
        }

        // Incrementar versão para garantir que qualquer chave residual expire
        Interlocked.Increment(ref _version);

        return Task.CompletedTask;
    }

    public Task InvalidateAllAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _version);
        return Task.CompletedTask;
    }

    private string BuildVersionedKey(string cacheKey)
        => $"{CachePrefix}v{Interlocked.Read(ref _version)}:{cacheKey}";
}
