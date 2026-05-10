using System.Text;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

/// <summary>
/// AI-0.2: Implementação de ITokenQuotaCache usando IDistributedCache (Redis quando disponível).
/// Janela de cache: 1 minuto (configurável via TTL passado pelo caller).
/// Fallback gracioso para miss silencioso em caso de falha do Redis.
/// Formato de chave: token-quota:{granularity}:{userId}
/// </summary>
public sealed class DistributedTokenQuotaCache(
    IDistributedCache distributedCache,
    ILogger<DistributedTokenQuotaCache> logger) : ITokenQuotaCache
{
    private static readonly DistributedCacheEntryOptions DefaultOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
    };

    public async Task<long?> GetUsageAsync(string userId, string granularity, CancellationToken ct = default)
    {
        try
        {
            var key = BuildKey(userId, granularity);
            var bytes = await distributedCache.GetAsync(key, ct);

            if (bytes is null || bytes.Length == 0)
                return null;

            var value = Encoding.UTF8.GetString(bytes);
            return long.TryParse(value, out var parsed) ? parsed : null;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "DistributedTokenQuotaCache miss — cache indisponível para user={UserId}.", userId);
            return null;
        }
    }

    public async Task SetUsageAsync(string userId, string granularity, long tokens, TimeSpan ttl, CancellationToken ct = default)
    {
        try
        {
            var key = BuildKey(userId, granularity);
            var bytes = Encoding.UTF8.GetBytes(tokens.ToString());
            var opts = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl > TimeSpan.Zero ? ttl : DefaultOptions.AbsoluteExpirationRelativeToNow
            };

            await distributedCache.SetAsync(key, bytes, opts, ct);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "DistributedTokenQuotaCache set falhou — a ignorar (degraded mode).");
        }
    }

    public async Task InvalidateUserAsync(string userId, CancellationToken ct = default)
    {
        // Tenta remover entradas conhecidas de granularidade
        var granularities = new[] { "daily", "monthly", "hourly", "weekly" };

        foreach (var granularity in granularities)
        {
            try
            {
                await distributedCache.RemoveAsync(BuildKey(userId, granularity), ct);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "DistributedTokenQuotaCache invalidate falhou para user={UserId} granularity={Granularity}.", userId, granularity);
            }
        }
    }

    private static string BuildKey(string userId, string granularity)
        => $"token-quota:{granularity}:{userId}";
}
