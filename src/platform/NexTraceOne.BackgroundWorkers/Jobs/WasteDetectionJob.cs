using MediatR;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NexTraceOne.Catalog.Contracts.Graph.ServiceInterfaces;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Cost.Features.DetectWasteSignals;

namespace NexTraceOne.BackgroundWorkers.Jobs;

/// <summary>
/// W6-01: Detecta sinais de desperdício operacional diariamente para todos os serviços registados.
///
/// Comportamento:
/// - Intervalo: 24 horas (com delay inicial de 90s para estabilização).
/// - Lista todos os serviços do Catálogo via ICatalogGraphModule.
/// - Executa DetectWasteSignals por serviço — falhas individuais não interrompem o ciclo.
/// - Após detecção, notifica team owners por grupo de desperdício encontrado.
///
/// Tipos de desperdício detectados (via DetectWasteSignals.Command):
/// - Serviços acima do budget (IdleResources, Overprovisioned, OrphanedResources)
/// - Serviços sem perfil de custo mas com actividade (OverlappingServices)
/// </summary>
public sealed class WasteDetectionJob(
    IServiceScopeFactory serviceScopeFactory,
    WorkerJobHealthRegistry jobHealthRegistry,
    ILogger<WasteDetectionJob> logger) : BackgroundService
{
    internal const string HealthCheckName = "waste-detection-job";

    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private static readonly TimeSpan StartDelay = TimeSpan.FromSeconds(90);
    private static readonly string[] DefaultEnvironments = ["production", "staging"];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        jobHealthRegistry.MarkStarted(HealthCheckName);
        logger.LogInformation("WasteDetectionJob iniciado — intervalo {Interval}.", Interval);

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
                await RunDetectionCycleAsync(stoppingToken);
                jobHealthRegistry.MarkSucceeded(HealthCheckName);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                jobHealthRegistry.MarkFailed(HealthCheckName, "Ciclo de detecção de desperdício falhou.");
                logger.LogError(ex, "Erro no ciclo do WasteDetectionJob.");
            }
        }

        logger.LogInformation("WasteDetectionJob parado.");
    }

    internal async Task RunDetectionCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var catalogModule = scope.ServiceProvider.GetRequiredService<ICatalogGraphModule>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var wasteRepo = scope.ServiceProvider.GetRequiredService<IWasteSignalRepository>();
        var notificationModule = scope.ServiceProvider.GetService<INotificationModule>();

        var services = await catalogModule.ListAllServicesAsync(cancellationToken);

        if (services.Count == 0)
        {
            logger.LogDebug("WasteDetectionJob: sem serviços no catálogo — a ignorar ciclo.");
            return;
        }

        logger.LogInformation("WasteDetectionJob: a analisar {Count} serviços em {EnvCount} ambientes.",
            services.Count, DefaultEnvironments.Length);

        var totalDetected = 0;
        var failedServices = 0;

        foreach (var service in services)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            foreach (var environment in DefaultEnvironments)
            {
                try
                {
                    var result = await mediator.Send(
                        new DetectWasteSignals.Command(service.Name, environment),
                        cancellationToken);

                    if (result.IsSuccess && result.Value.DetectedCount > 0)
                    {
                        totalDetected += result.Value.DetectedCount;
                        logger.LogInformation(
                            "WasteDetectionJob: {Count} sinais detectados em '{Service}/{Env}'.",
                            result.Value.SignalsCreated, service.Name, environment);
                    }
                }
                catch (Exception ex)
                {
                    failedServices++;
                    logger.LogWarning(ex,
                        "WasteDetectionJob: erro ao analisar '{Service}/{Env}' — a continuar.",
                        service.Name, environment);
                }
            }
        }

        logger.LogInformation(
            "WasteDetectionJob: ciclo concluído. {Total} sinais detectados, {Failed} falhas.",
            totalDetected, failedServices);

        if (totalDetected > 0)
            await NotifyTeamOwnersAsync(wasteRepo, notificationModule, cancellationToken);
    }

    private async Task NotifyTeamOwnersAsync(
        IWasteSignalRepository wasteRepo,
        INotificationModule? notificationModule,
        CancellationToken cancellationToken)
    {
        if (notificationModule is null)
            return;

        try
        {
            var allSignals = await wasteRepo.ListAllAsync(teamName: null, includeAcknowledged: false, ct: cancellationToken);
            var recentSignals = allSignals
                .Where(s => s.DetectedAt >= DateTimeOffset.UtcNow.AddHours(-25))
                .ToList();

            if (recentSignals.Count == 0)
                return;

            var teamGroups = recentSignals
                .GroupBy(s => s.TeamName ?? "Unknown")
                .ToList();

            foreach (var group in teamGroups)
            {
                var teamName = group.Key;
                var signalCount = group.Count();
                var estimatedSavings = group.Sum(s => s.EstimatedMonthlySavings);

                await notificationModule.SubmitAsync(new NotificationRequest
                {
                    EventType = "WasteDetected",
                    Category = "FinOps",
                    Severity = "Warning",
                    Title = $"Desperdício detectado: {signalCount} sinais para equipa '{teamName}'",
                    Message = $"Foram detectados {signalCount} sinais de desperdício operacional para a equipa '{teamName}'. " +
                              $"Poupança estimada: {estimatedSavings:F2} USD/mês. " +
                              $"Aceda ao FinOps Center para detalhes e ações recomendadas.",
                    SourceModule = "OperationalIntelligence",
                    SourceEntityType = "WasteSignal",
                    RecipientRoles = ["TechLead"],
                    ActionUrl = "/finops/waste",
                    RequiresAction = estimatedSavings > 500,
                }, cancellationToken);

                logger.LogInformation(
                    "WasteDetectionJob: notificação enviada para equipa '{Team}' ({Count} sinais, ~{Savings:F0} USD/mês).",
                    teamName, signalCount, estimatedSavings);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "WasteDetectionJob: erro ao notificar team owners.");
        }
    }
}
