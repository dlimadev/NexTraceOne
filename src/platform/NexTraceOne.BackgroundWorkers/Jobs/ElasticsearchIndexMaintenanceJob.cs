using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NexTraceOne.BackgroundWorkers.Elasticsearch;

namespace NexTraceOne.BackgroundWorkers.Jobs;

/// <summary>
/// W7-01: Aplica políticas ILM ao cluster Elasticsearch periodicamente.
///
/// Comportamento:
/// - Intervalo: 6 horas (com delay inicial de 120s).
/// - Se o cluster não estiver disponível, ignora o ciclo e regista aviso.
/// - Falhas individuais por política não interrompem as restantes.
/// </summary>
public sealed class ElasticsearchIndexMaintenanceJob(
    IServiceScopeFactory serviceScopeFactory,
    WorkerJobHealthRegistry jobHealthRegistry,
    ILogger<ElasticsearchIndexMaintenanceJob> logger) : BackgroundService
{
    internal const string HealthCheckName = "elasticsearch-index-maintenance-job";

    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);
    private static readonly TimeSpan StartDelay = TimeSpan.FromSeconds(120);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        jobHealthRegistry.MarkStarted(HealthCheckName);
        logger.LogInformation("ElasticsearchIndexMaintenanceJob iniciado — intervalo {Interval}.", Interval);

        try
        {
            await Task.Delay(StartDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(Interval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                jobHealthRegistry.MarkStarted(HealthCheckName);
                await RunMaintenanceCycleAsync(stoppingToken);
                jobHealthRegistry.MarkSucceeded(HealthCheckName);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                jobHealthRegistry.MarkFailed(HealthCheckName, "Manutenção ILM do Elasticsearch falhou.");
                logger.LogError(ex, "Erro no ciclo do ElasticsearchIndexMaintenanceJob.");
            }
        }

        logger.LogInformation("ElasticsearchIndexMaintenanceJob parado.");
    }

    internal async Task RunMaintenanceCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var indexManager = scope.ServiceProvider.GetRequiredService<IElasticsearchIndexManager>();

        var healthy = await indexManager.IsClusterHealthyAsync(cancellationToken);
        if (!healthy)
        {
            logger.LogDebug(
                "ElasticsearchIndexMaintenanceJob: cluster Elasticsearch não acessível — a ignorar ciclo.");
            return;
        }

        logger.LogInformation("ElasticsearchIndexMaintenanceJob: a aplicar políticas ILM...");
        await indexManager.ApplyIlmPoliciesAsync(cancellationToken);
        logger.LogInformation("ElasticsearchIndexMaintenanceJob: ciclo de manutenção ILM concluído.");
    }
}
