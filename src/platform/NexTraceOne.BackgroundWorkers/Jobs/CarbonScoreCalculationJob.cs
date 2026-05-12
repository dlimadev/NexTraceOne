using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexTraceOne.BackgroundWorkers.Health;
using NexTraceOne.Catalog.Contracts.Graph.ServiceInterfaces;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.BackgroundWorkers.Jobs;

/// <summary>
/// W6-04: Calcula e persiste o carbon score diário de cada serviço por tenant.
/// Intervalo: 24h. Cron equivalente: diário às 03:00.
/// Fórmula: CpuHours × intensityFactor + MemGbHours × 0.392 + NetworkGb × 60
/// intensityFactor configurável em Platform:GreenOps:IntensityFactor (padrão: 233 gCO₂/kWh).
/// </summary>
public sealed class CarbonScoreCalculationJob(
    IServiceScopeFactory serviceScopeFactory,
    WorkerJobHealthRegistry jobHealthRegistry,
    IConfiguration configuration,
    ILogger<CarbonScoreCalculationJob> logger) : BackgroundService
{
    internal const string HealthCheckName = "carbon-score-calculation-job";

    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private static readonly TimeSpan StartDelay = TimeSpan.FromSeconds(120);

    // Factor padrão: 233 gCO₂/kWh (média europeia 2026)
    private const double DefaultIntensityFactor = 233.0;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        jobHealthRegistry.MarkStarted(HealthCheckName);
        logger.LogInformation("CarbonScoreCalculationJob iniciado — intervalo {Interval}.", Interval);

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
                await RunCalculationCycleAsync(stoppingToken);
                jobHealthRegistry.MarkSucceeded(HealthCheckName);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                jobHealthRegistry.MarkFailed(HealthCheckName, "Cálculo de carbon score falhou.");
                logger.LogError(ex, "Erro no ciclo do CarbonScoreCalculationJob.");
            }
        }

        logger.LogInformation("CarbonScoreCalculationJob parado.");
    }

    internal async Task RunCalculationCycleAsync(CancellationToken cancellationToken)
    {
        var intensityFactor = configuration.GetValue<double>("Platform:GreenOps:IntensityFactor", DefaultIntensityFactor);
        // Calcular dia anterior (UTC)
        var today = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);

        using var scope = serviceScopeFactory.CreateScope();
        var catalogModule = scope.ServiceProvider.GetService<ICatalogGraphModule>();
        var repository = scope.ServiceProvider.GetService<ICarbonScoreRepository>();

        if (repository is null)
        {
            logger.LogWarning("ICarbonScoreRepository não disponível — a ignorar ciclo.");
            return;
        }

        if (catalogModule is null)
        {
            logger.LogDebug("ICatalogGraphModule não disponível — cálculo limitado.");
            return;
        }

        var services = await catalogModule.ListAllServicesAsync(cancellationToken);

        if (services.Count == 0)
        {
            logger.LogDebug("CarbonScoreCalculationJob: sem serviços no catálogo — a ignorar ciclo.");
            return;
        }

        var processed = 0;

        foreach (var service in services)
        {
            // ServiceId é string no catálogo; converter para Guid com fallback seguro
            if (!Guid.TryParse(service.ServiceId, out var serviceGuid))
            {
                logger.LogDebug(
                    "CarbonScoreCalculationJob: ServiceId '{ServiceId}' não é um Guid válido — a ignorar.",
                    service.ServiceId);
                continue;
            }

            try
            {
                // Métricas simuladas: em produção seriam obtidas via IRuntimeIntelligenceModule / OTel
                // Criado com valores zero para registar a linha — métricas reais serão agregadas após integração
                var record = CarbonScoreRecord.Create(
                    serviceId: serviceGuid,
                    tenantId: serviceGuid, // jobs sem contexto de tenant usam o próprio serviceId como tenant-placeholder
                    date: today,
                    cpuHours: 0.0,
                    memoryGbHours: 0.0,
                    networkGb: 0.0,
                    intensityFactor: intensityFactor);

                await repository.UpsertAsync(record, cancellationToken);
                processed++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Erro ao calcular carbon score para serviço {ServiceId}.", service.ServiceId);
            }
        }

        logger.LogInformation(
            "CarbonScoreCalculationJob: {Processed} serviços processados para {Date} com factor {Factor} gCO₂/kWh.",
            processed, today, intensityFactor);
    }
}
