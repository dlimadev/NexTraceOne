using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Configuration;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

/// <summary>
/// Cache in-process para uso de quota de tokens — evita DB calls repetidos dentro da mesma janela.
/// Usa IMemoryCache com TTL configurável. Thread-safe via MemoryCache interna.
/// </summary>
public sealed class InMemoryTokenQuotaCache : ITokenQuotaCache, IDisposable
{
    private readonly MemoryCache _cache;
    private readonly TokenQuotaCacheOptions _options;

    public InMemoryTokenQuotaCache(IOptions<TokenQuotaCacheOptions> options)
    {
        _options = options.Value;
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = _options.MaxEntries
        });
    }

    public Task<long?> GetUsageAsync(string userId, string granularity, CancellationToken ct = default)
    {
        var key = BuildUsageKey(userId, granularity);
        _cache.TryGetValue(key, out long? value);
        return Task.FromResult(value);
    }

    public Task SetUsageAsync(string userId, string granularity, long tokens, TimeSpan ttl, CancellationToken ct = default)
    {
        var key = BuildUsageKey(userId, granularity);
        var entry = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(ttl)
            .SetSize(1);
        _cache.Set(key, (long?)tokens, entry);
        return Task.CompletedTask;
    }

    public Task InvalidateUserAsync(string userId, CancellationToken ct = default)
    {
        _cache.Remove(BuildUsageKey(userId, "daily"));
        _cache.Remove(BuildUsageKey(userId, "monthly"));
        return Task.CompletedTask;
    }

    public void Dispose() => _cache.Dispose();

    private static string BuildUsageKey(string userId, string granularity)
        => $"quota:usage:{granularity}:{userId}";
}
