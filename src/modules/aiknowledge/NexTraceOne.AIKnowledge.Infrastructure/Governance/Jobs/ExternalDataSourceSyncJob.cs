using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Jobs;

/// <summary>
/// Job periódico que sincroniza automaticamente fontes de dados externas elegíveis.
/// Apenas processa fontes com SyncIntervalMinutes &gt; 0 e cuja janela de sync expirou.
///
/// Design:
/// - BackgroundService com PeriodicTimer (execução a cada 15 minutos).
/// - Cria scope por ciclo para isolar DbContext e serviços Scoped.
/// - Falhas por fonte são tratadas individualmente e registadas no LastSyncError.
/// </summary>
internal sealed class ExternalDataSourceSyncJob(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<ExternalDataSourceSyncJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ExternalDataSourceSyncJob started.");

        await Task.Delay(TimeSpan.FromSeconds(120), stoppingToken);

        using var timer = new PeriodicTimer(Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in ExternalDataSourceSyncJob cycle.");
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task RunCycleAsync(CancellationToken ct)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IExternalDataSourceRepository>();
        var syncService = scope.ServiceProvider.GetRequiredService<IDataSourceSyncService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var now = DateTimeOffset.UtcNow;
        var sources = await repository.ListDueForSyncAsync(now, ct);

        if (sources.Count == 0) return;

        logger.LogInformation("ExternalDataSourceSyncJob: {Count} sources due for sync.", sources.Count);

        foreach (var source in sources)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                logger.LogInformation("ExternalDataSourceSyncJob: syncing '{Name}'.", source.Name);
                var result = await syncService.SyncAsync(source, ct);

                await unitOfWork.CommitAsync(ct);

                logger.LogInformation(
                    "ExternalDataSourceSyncJob: '{Name}' — success={Success}, docs={Count}.",
                    source.Name, result.Success, result.DocumentsIndexed);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ExternalDataSourceSyncJob: failed to sync '{Name}'.", source.Name);
            }
        }
    }
}
