using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.BackgroundWorkers;
using NexTraceOne.BackgroundWorkers.Configuration;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Contracts.IntegrationEvents;

namespace NexTraceOne.BackgroundWorkers.Jobs;

/// <summary>
/// Job periódico que cruza serviços descobertos via telemetria OTel (DiscoveredService em estado
/// Pending com alto tráfego) e publica UncatalogedServiceDetectedIntegrationEvent para cada um.
///
/// Permite que o módulo de notificações alerte os platform engineers sobre serviços não catalogados
/// com tráfego significativo, e que o módulo de knowledge os indexe para pesquisa.
///
/// Design:
/// - Executa com intervalo configurável (padrão: 30 minutos).
/// - Limiar de tráfego e limite de serviços por ciclo são configuráveis via OtelCatalogBridgeOptions.
/// - Falhas individuais são logadas sem interromper o ciclo.
/// </summary>
public sealed class OtelCatalogBridgeJob(
    IServiceScopeFactory serviceScopeFactory,
    WorkerJobHealthRegistry jobHealthRegistry,
    ILogger<OtelCatalogBridgeJob> logger) : BackgroundService
{
    internal const string HealthCheckName = "otel-catalog-bridge-job";

    private static readonly TimeSpan StartDelay = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        jobHealthRegistry.MarkStarted(HealthCheckName);
        logger.LogInformation("OtelCatalogBridgeJob started.");

        try
        {
            await Task.Delay(StartDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var scope = serviceScopeFactory.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<OtelCatalogBridgeOptions>>().Value;
        using var timer = new PeriodicTimer(options.IntervalBetweenCycles);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                jobHealthRegistry.MarkStarted(HealthCheckName);
                await RunBridgeCycleAsync(stoppingToken);
                jobHealthRegistry.MarkSucceeded(HealthCheckName);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                jobHealthRegistry.MarkFailed(HealthCheckName, ex.Message);
                logger.LogError(ex, "OtelCatalogBridgeJob cycle failed.");
            }
        }
    }

    private async Task RunBridgeCycleAsync(CancellationToken ct)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<OtelCatalogBridgeOptions>>().Value;

        if (!options.Enabled)
        {
            logger.LogDebug("OtelCatalogBridgeJob is disabled by configuration. Skipping cycle.");
            return;
        }

        var discoveredServiceRepo = scope.ServiceProvider.GetRequiredService<IDiscoveredServiceRepository>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<ICatalogGraphUnitOfWork>();

        var uncataloged = await discoveredServiceRepo.ListPendingWithHighTrafficAsync(
            options.MinTraceCountToAlert,
            options.MaxServicesToProcessPerCycle,
            ct);

        if (uncataloged.Count == 0)
        {
            logger.LogDebug("OtelCatalogBridgeJob: nenhum serviço de alto tráfego sem catálogo encontrado.");
            return;
        }

        logger.LogInformation(
            "OtelCatalogBridgeJob: {Count} serviço(s) de alto tráfego sem catálogo detectado(s).",
            uncataloged.Count);

        foreach (var svc in uncataloged)
        {
            await eventBus.PublishAsync(new UncatalogedServiceDetectedIntegrationEvent(
                ServiceName: svc.ServiceName,
                ServiceNamespace: svc.ServiceNamespace,
                Environment: svc.Environment,
                TraceCount: svc.TraceCount,
                FirstSeenAt: svc.FirstSeenAt,
                LastSeenAt: svc.LastSeenAt,
                TenantId: null), ct);
        }

        await unitOfWork.CommitAsync(ct);

        logger.LogInformation(
            "OtelCatalogBridgeJob: {Count} evento(s) publicado(s) via outbox.",
            uncataloged.Count);
    }
}
