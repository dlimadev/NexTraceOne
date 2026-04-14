using Microsoft.Extensions.Caching.Memory;

using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Infrastructure.Services;

/// <summary>
/// Implementação de cache de configurações usando IMemoryCache.
/// Chaves prefixadas com "cfg:" para isolamento. Expiração padrão de 5 minutos.
/// Invalidação global via contador de versão para evitar enumerar chaves.
/// Protege contra cache de resultados cancelados ou falhados.
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

    public async Task<T> GetOrSetAsync<T>(string cacheKey, Func<Task<T>> factory, CancellationToken cancellationToken)
    {
        var versionedKey = BuildVersionedKey(cacheKey);

        if (memoryCache.TryGetValue(versionedKey, out T? cached))
        {
            return cached!;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var value = await factory();

        cancellationToken.ThrowIfCancellationRequested();

        using var entry = memoryCache.CreateEntry(versionedKey);
        entry.AbsoluteExpirationRelativeToNow = DefaultExpiration;
        entry.Value = value;

        return value;
    }

    public Task InvalidateAsync(string key, ConfigurationScope? scope, CancellationToken cancellationToken)
    {
        // IMemoryCache does not support prefix/pattern-based eviction.
        // Incrementing the version counter causes all existing versioned keys
        // to become cache misses, effectively invalidating every cached entry.
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
