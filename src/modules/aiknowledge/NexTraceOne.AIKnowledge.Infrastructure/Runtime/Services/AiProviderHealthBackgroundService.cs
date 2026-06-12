using System.Collections.Concurrent;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

/// <summary>
/// Serviço em background que verifica periodicamente a saúde de todos os providers de IA
/// e mantém um cache singleton atualizado para consulta rápida sem overhead de I/O.
/// </summary>
public sealed class AiProviderHealthBackgroundService : BackgroundService, IAiProviderHealthMonitor
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<AiProviderHealthBackgroundService> _logger;
    private readonly ConcurrentDictionary<string, AiProviderHealthResult> _healthCache = new(StringComparer.OrdinalIgnoreCase);
    private DateTimeOffset _lastCheckTime;

    public AiProviderHealthBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<AiProviderHealthBackgroundService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public DateTimeOffset? LastCheckTime => _lastCheckTime == default ? null : _lastCheckTime;

    public IReadOnlyList<AiProviderHealthResult> GetAllHealthStatuses()
        => _healthCache.Values.ToList();

    public AiProviderHealthResult? GetHealthStatus(string providerId)
        => _healthCache.TryGetValue(providerId, out var result) ? result : null;

    public bool IsHealthy(string providerId)
        => _healthCache.TryGetValue(providerId, out var result) && result.IsHealthy;

    public IReadOnlyList<string> GetHealthyProviderIds()
        => _healthCache
            .Where(kv => kv.Value.IsHealthy)
            .Select(kv => kv.Key)
            .ToList();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AiProviderHealthBackgroundService started. Polling interval: 30s.");

        // First check immediately
        try
        {
            await RunHealthCheckCycleAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initial health check cycle failed.");
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunHealthCheckCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in health check cycle.");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
                break;
        }

        _logger.LogInformation("AiProviderHealthBackgroundService stopped.");
    }

    private async Task RunHealthCheckCycleAsync(CancellationToken ct)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var healthService = scope.ServiceProvider.GetRequiredService<IAiProviderHealthService>();

        IReadOnlyList<AiProviderHealthResult> results;
        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, ct);
            results = await healthService.CheckAllProvidersAsync(linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Health check cycle timed out.");
            return;
        }

        foreach (var result in results)
        {
            _healthCache[result.ProviderId] = result;
        }

        // Remove providers that are no longer registered
        var currentIds = results.Select(r => r.ProviderId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var key in _healthCache.Keys.ToList())
        {
            if (!currentIds.Contains(key))
                _healthCache.TryRemove(key, out _);
        }

        _lastCheckTime = DateTimeOffset.UtcNow;

        var healthy = results.Count(r => r.IsHealthy);
        _logger.LogDebug(
            "Health check cycle complete. {Healthy}/{Total} providers healthy. Checked at {Timestamp}",
            healthy, results.Count, _lastCheckTime);
    }
}
