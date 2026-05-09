using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;

namespace NexTraceOne.BackgroundWorkers.Jobs;

/// <summary>
/// SaaS-09: Recalcula periodicamente CurrentHostUnits de cada TenantLicense
/// somando os HostUnits dos AgentRegistrations activos reportados pelos agentes NexTrace.
///
/// Comportamento:
/// - Arranque com 45 segundos de delay para aguardar estabilização da base de dados.
/// - Ciclos de 15 minutos — frequência suficiente para billing sem pressão excessiva.
/// - Omite actualizações com variação inferior a 0.1 HU para evitar writes desnecessários.
/// - Falhas isoladas por tenant não interrompem o ciclo dos demais.
/// </summary>
public sealed class LicenseRecalculationJob(
    IServiceScopeFactory serviceScopeFactory,
    IDateTimeProvider dateTimeProvider,
    WorkerJobHealthRegistry jobHealthRegistry,
    ILogger<LicenseRecalculationJob> logger) : BackgroundService
{
    internal const string HealthCheckName = "license-recalculation-job";

    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan StartDelay = TimeSpan.FromSeconds(45);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        jobHealthRegistry.MarkStarted(HealthCheckName);
        logger.LogInformation("LicenseRecalculationJob started — interval {Interval}.", Interval);

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
                await RecalculateAllAsync(stoppingToken);
                jobHealthRegistry.MarkSucceeded(HealthCheckName);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                jobHealthRegistry.MarkFailed(HealthCheckName, "License recalculation cycle failed.");
                logger.LogError(ex, "Unhandled error in LicenseRecalculationJob cycle.");
            }
        }

        logger.LogInformation("LicenseRecalculationJob stopped.");
    }

    private async Task RecalculateAllAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var licenseRepo = scope.ServiceProvider.GetRequiredService<ITenantLicenseRepository>();
        var agentRepo = scope.ServiceProvider.GetRequiredService<IAgentRegistrationRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IIdentityAccessUnitOfWork>();

        var licenses = await licenseRepo.ListAllAsync(cancellationToken);

        if (licenses.Count == 0)
        {
            logger.LogDebug("LicenseRecalculationJob: nenhuma licença encontrada.");
            return;
        }

        var now = dateTimeProvider.UtcNow;
        var updatedCount = 0;

        foreach (var license in licenses)
        {
            var activeHostUnits = await agentRepo.SumActiveHostUnitsAsync(license.TenantId, cancellationToken);

            if (Math.Abs(activeHostUnits - license.CurrentHostUnits) < 0.1m)
                continue;

            license.UpdateHostUnits(activeHostUnits, now);
            licenseRepo.Update(license);
            updatedCount++;
        }

        if (updatedCount == 0)
        {
            logger.LogDebug(
                "LicenseRecalculationJob: sem alterações em {Total} licenças.",
                licenses.Count);
            return;
        }

        await unitOfWork.CommitAsync(cancellationToken);

        logger.LogInformation(
            "LicenseRecalculationJob: {Updated}/{Total} licenças actualizadas.",
            updatedCount, licenses.Count);
    }
}
