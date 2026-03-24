using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Application.Abstractions;

/// <summary>
/// Service abstraction for caching configuration resolution results.
/// Reduces database round-trips for frequently accessed configuration values.
/// Implementation provided by the Infrastructure layer.
/// </summary>
public interface IConfigurationCacheService
{
    /// <summary>Returns cached value or invokes the factory, caches, and returns the result.</summary>
    Task<T> GetOrSetAsync<T>(string cacheKey, Func<Task<T>> factory, CancellationToken cancellationToken);

    /// <summary>
    /// Invalidates cache entries for a specific key and optional scope.
    /// When scope is null, all scope-level cache entries for the key are invalidated.
    /// </summary>
    Task InvalidateAsync(string key, ConfigurationScope? scope, CancellationToken cancellationToken);

    /// <summary>Invalidates all configuration cache entries.</summary>
    Task InvalidateAllAsync(CancellationToken cancellationToken);
}
