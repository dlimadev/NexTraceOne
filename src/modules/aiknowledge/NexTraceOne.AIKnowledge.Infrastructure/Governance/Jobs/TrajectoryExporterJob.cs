using MediatR;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Features.ExportPendingTrajectories;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Jobs;

/// <summary>
/// Job de exportação de trajectórias para Agent Lightning trainer.
/// Corre a cada 15 minutos, exporta trajectórias com feedback confirmado para ficheiros JSON.
///
/// Design:
/// - BackgroundService com delay loop (execução a cada 15 minutos).
/// - Cria scope por ciclo para isolar DbContext e serviços Scoped.
/// - Exporta até 50 trajectórias por ciclo para o directório configurado.
/// </summary>
internal sealed class TrajectoryExporterJob(
    IServiceScopeFactory scopeFactory,
    ILogger<TrajectoryExporterJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("TrajectoryExporterJob started.");

        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var exportDir = Path.Combine(Path.GetTempPath(), "nextrace_trajectories");

                var result = await sender.Send(
                    new ExportPendingTrajectories.Command(50, exportDir, null),
                    stoppingToken);

                if (result.IsSuccess && result.Value.ExportedCount > 0)
                    logger.LogInformation(
                        "Exported {Count} trajectories for Agent Lightning.",
                        result.Value.ExportedCount);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Trajectory export failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }

        logger.LogInformation("TrajectoryExporterJob stopped.");
    }
}
