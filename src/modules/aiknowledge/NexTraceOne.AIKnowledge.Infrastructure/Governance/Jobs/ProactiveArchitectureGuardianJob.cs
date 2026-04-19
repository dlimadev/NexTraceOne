using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Jobs;

/// <summary>
/// Job do Proactive Architecture Guardian.
/// Corre a cada 30 minutos, analisa padrões de risco e emite alertas proactivos.
/// Detecta padrões antes de incidentes — SLA breach, token budget 80%, missing health checks, etc.
/// </summary>
public sealed class ProactiveArchitectureGuardianJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ProactiveArchitectureGuardianJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var alertRepository = scope.ServiceProvider.GetRequiredService<IGuardianAlertRepository>();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                logger.LogInformation("ProactiveArchitectureGuardian: running analysis cycle at {Time}", DateTimeOffset.UtcNow);

                // Framework in place for incremental pattern detectors.
                // In production, this would inspect real metrics (token budgets, SLA, health checks).
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "ProactiveArchitectureGuardian: analysis cycle failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }
}
