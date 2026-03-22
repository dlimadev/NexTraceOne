using MediatR;
using Microsoft.Extensions.Options;
using NexTraceOne.BackgroundWorkers.Configuration;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.DetectRuntimeDrift;

namespace NexTraceOne.BackgroundWorkers.Jobs;

/// <summary>
/// Job periódico que executa drift detection automaticamente para todos os serviços
/// e ambientes configurados. Compara o snapshot mais recente de cada serviço com a
/// sua baseline, persiste os findings e registra o resultado operacional.
///
/// Design:
/// - Executa em frequência configurável (padrão: a cada 5 minutos).
/// - Processa cada combinação serviço/ambiente de forma isolada.
/// - Falhas individuais são logadas sem interromper o ciclo completo.
/// - Observável via WorkerJobHealthRegistry (health checks e status).
/// </summary>
public sealed class DriftDetectionJob(
    IServiceScopeFactory serviceScopeFactory,
    WorkerJobHealthRegistry jobHealthRegistry,
    ILogger<DriftDetectionJob> logger) : BackgroundService
{
    internal const string HealthCheckName = "drift-detection";

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        jobHealthRegistry.MarkStarted(HealthCheckName);

        // Executa o primeiro ciclo imediatamente na inicialização (no-op se disabled).
        // A partir daí entra no loop de intervalo configurado.
        await RunDriftDetectionCycleAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = serviceScopeFactory.CreateScope();
            var options = scope.ServiceProvider.GetRequiredService<IOptions<DriftDetectionOptions>>().Value;

            if (!options.Enabled)
            {
                logger.LogDebug("DriftDetectionJob is disabled by configuration. Sleeping.");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                continue;
            }

            try
            {
                await Task.Delay(options.IntervalBetweenCycles, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (!stoppingToken.IsCancellationRequested)
            {
                await RunDriftDetectionCycleAsync(stoppingToken);
            }
        }
    }

    private async Task RunDriftDetectionCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<DriftDetectionOptions>>().Value;

        if (!options.Enabled)
            return;

        jobHealthRegistry.MarkStarted(HealthCheckName);

        try
        {
            var snapshotRepository = scope.ServiceProvider.GetRequiredService<IRuntimeSnapshotRepository>();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            // Obtém os serviços com snapshots recentes dentro da janela de análise
            var since = DateTimeOffset.UtcNow - options.AnalysisWindow;
            var recentServices = await snapshotRepository.GetServicesWithRecentSnapshotsAsync(since, cancellationToken);

            if (recentServices.Count == 0)
            {
                logger.LogDebug("DriftDetectionJob: no services with recent snapshots found. Skipping cycle.");
                jobHealthRegistry.MarkSucceeded(HealthCheckName);
                return;
            }

            var totalFindings = 0;
            var failedServices = 0;

            foreach (var (serviceName, environment) in recentServices)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Verifica se o ambiente está na lista de ambientes comparáveis
                if (options.ComparableEnvironments.Count > 0 &&
                    !options.ComparableEnvironments.Contains(environment, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    var command = new DetectRuntimeDrift.Command(
                        serviceName,
                        environment,
                        options.TolerancePercent);

                    var result = await mediator.Send(command, cancellationToken);

                    if (result.IsSuccess && result.Value.HasDrift)
                    {
                        totalFindings += result.Value.Findings.Count;
                        logger.LogWarning(
                            "DriftDetectionJob: drift detected for {ServiceName}/{Environment}. Findings: {FindingCount}",
                            serviceName, environment, result.Value.Findings.Count);
                    }
                    else if (!result.IsSuccess)
                    {
                        logger.LogDebug(
                            "DriftDetectionJob: could not run drift detection for {ServiceName}/{Environment}: {Error}",
                            serviceName, environment, result.Error?.Message);
                    }
                }
                catch (Exception ex)
                {
                    failedServices++;
                    logger.LogError(ex, "DriftDetectionJob: error processing {ServiceName}/{Environment}.", serviceName, environment);
                }
            }

            logger.LogInformation(
                "DriftDetectionJob cycle complete. Services checked: {ServiceCount}, Findings: {FindingCount}, Failures: {FailureCount}",
                recentServices.Count, totalFindings, failedServices);

            jobHealthRegistry.MarkSucceeded(HealthCheckName);
        }
        catch (Exception ex)
        {
            jobHealthRegistry.MarkFailed(HealthCheckName, "Drift detection cycle failed.");
            logger.LogError(ex, "DriftDetectionJob: unhandled error in drift detection cycle.");
        }
    }
}
