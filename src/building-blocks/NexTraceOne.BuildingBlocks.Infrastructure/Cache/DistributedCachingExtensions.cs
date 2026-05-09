using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Cache;

/// <summary>
/// Regista IDistributedCache com Redis quando a connection string está configurada,
/// ou com um wrapper em memória (AddDistributedMemoryCache) caso contrário.
/// Regista também IMemoryCache para os componentes que ainda dependem do cache em processo.
/// </summary>
public static class DistributedCachingExtensions
{
    private const string RedisSectionKey = "ConnectionStrings:Redis";

    public static IServiceCollection AddDistributedCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMemoryCache();

        var redisConnectionString = configuration[RedisSectionKey];

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(opts =>
            {
                opts.Configuration = redisConnectionString;
                opts.InstanceName = "nxt:";
            });
        }
        else
        {
            // Fallback: in-process distributed cache — funciona em single-instance;
            // substituir por Redis em deployments multi-instância.
            services.AddDistributedMemoryCache();
        }

        return services;
    }
}
