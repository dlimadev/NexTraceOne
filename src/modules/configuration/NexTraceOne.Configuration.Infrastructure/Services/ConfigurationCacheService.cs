using Microsoft.Extensions.Caching.Distributed;

using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

using System.Text.Json;

namespace NexTraceOne.Configuration.Infrastructure.Services;

/// <summary>
/// Implementação de cache de configurações usando IDistributedCache.
/// Suporta Redis (multi-instância) e in-process memory (single-instância).
/// Chaves prefixadas com "cfg:" para isolamento. Expiração padrão de 5 minutos.
/// Invalidação global via contador de versão armazenado no próprio cache distribuído,
/// garantindo consistência entre múltiplas instâncias da aplicação.
/// </summary>
internal sealed class ConfigurationCacheService(IDistributedCache distributedCache) : IConfigurationCacheService
{
    private const string CachePrefix = "cfg:";
    private const string VersionKey = "cfg:global:version";
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);

    public async Task<T> GetOrSetAsync<T>(string cacheKey, Func<Task<T>> factory, CancellationToken cancellationToken)
    {
        var version = await GetVersionAsync(cancellationToken);
        var versionedKey = $"{CachePrefix}v{version}:{cacheKey}";

        var bytes = await distributedCache.GetAsync(versionedKey, cancellationToken);
        if (bytes is not null)
        {
            return JsonSerializer.Deserialize<T>(bytes)!;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var value = await factory();

        cancellationToken.ThrowIfCancellationRequested();

        var serialized = JsonSerializer.SerializeToUtf8Bytes(value);
        await distributedCache.SetAsync(versionedKey, serialized, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = DefaultExpiration
        }, cancellationToken);

        return value;
    }

    public async Task InvalidateAsync(string key, ConfigurationScope? scope, CancellationToken cancellationToken)
        => await BumpVersionAsync(cancellationToken);

    public async Task InvalidateAllAsync(CancellationToken cancellationToken)
        => await BumpVersionAsync(cancellationToken);

    private async Task<long> GetVersionAsync(CancellationToken cancellationToken)
    {
        var bytes = await distributedCache.GetAsync(VersionKey, cancellationToken);
        return bytes is null ? 0L : BitConverter.ToInt64(bytes, 0);
    }

    private async Task BumpVersionAsync(CancellationToken cancellationToken)
    {
        var current = await GetVersionAsync(cancellationToken);
        var next = current + 1;
        await distributedCache.SetAsync(VersionKey, BitConverter.GetBytes(next), cancellationToken);
    }
}
