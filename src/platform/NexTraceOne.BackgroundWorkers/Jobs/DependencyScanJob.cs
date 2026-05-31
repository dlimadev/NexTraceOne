using MediatR;

using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BackgroundWorkers.Health;
using NexTraceOne.Catalog.Application.DependencyGovernance.Features.EnrichServiceDependencies;
using NexTraceOne.Catalog.Application.DependencyGovernance.Ports;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.BackgroundWorkers.Jobs;

/// <summary>
/// Job periódico que enriquece perfis de dependências com dados ao vivo de OSV e NuGet.org.
/// Roda a cada 6 horas por padrão.
/// </summary>
public sealed class DependencyScanJob(
    IServiceScopeFactory serviceScopeFactory,
    WorkerJobHealthRegistry jobHealthRegistry,
    ILogger<DependencyScanJob> logger) : BackgroundService
{
    internal const string HealthCheckName = "dependency-scan";
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromHours(6);
    private static readonly TimeSpan StartDelay = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        jobHealthRegistry.MarkStarted(HealthCheckName);
        logger.LogInformation("DependencyScanJob started. First scan in {Delay}.", StartDelay);

        await Task.Delay(StartDelay, stoppingToken);
        using var timer = new PeriodicTimer(DefaultInterval);

        while (!stoppingToken.IsCancellationRequested
               && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                jobHealthRegistry.MarkStarted(HealthCheckName);
                await RunScanCycleAsync(stoppingToken);
                jobHealthRegistry.MarkSucceeded(HealthCheckName);
            }
            catch (OperationCanceledException)
            {
                logger.LogDebug("DependencyScanJob cancelled.");
                throw;
            }
            catch (Exception ex)
            {
                jobHealthRegistry.MarkFailed(HealthCheckName, ex.Message);
                logger.LogError(ex, "DependencyScanJob cycle failed.");
            }
        }
    }

    private async Task RunScanCycleAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IServiceDependencyProfileRepository>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var dateTime = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        // Listar todos os perfis que não foram escaneados nas últimas 6h
        var cutoff = dateTime.UtcNow.AddHours(-6);
        var profiles = await repository.ListStaleProfilesAsync(cutoff, 100, stoppingToken);

        logger.LogInformation("DependencyScanJob: {Count} profiles to enrich.", profiles.Count);

        foreach (var profile in profiles)
        {
            stoppingToken.ThrowIfCancellationRequested();

            try
            {
                var result = await mediator.Send(
                    new EnrichServiceDependencies.Command(profile.ServiceId), stoppingToken);

                if (result.IsSuccess)
                {
                    logger.LogInformation(
                        "Enriched dependencies for service {ServiceId}: score={HealthScore}, vulns={VulnCount}, outdated={OutdatedCount}",
                        profile.ServiceId,
                        result.Value.HealthScore,
                        result.Value.VulnerabilityCount,
                        result.Value.OutdatedCount);
                }
                else
                {
                    logger.LogWarning(
                        "Failed to enrich dependencies for service {ServiceId}: {Error}",
                        profile.ServiceId,
                        result.Error.Message);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Exception enriching service {ServiceId}.", profile.ServiceId);
            }
        }
    }
}
