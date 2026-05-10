using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NexTraceOne.BackgroundWorkers.Health;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;

namespace NexTraceOne.BackgroundWorkers.Jobs;

/// <summary>
/// W2-03: Monitoriza a saúde da plataforma e envia alertas a PlatformAdmins.
///
/// Verificações a cada 5 minutos:
/// - Outbox pendente (Warning >500, Critical >2000)
/// - Uso de disco (Warning >80%, Critical >95%)
/// - Jobs sem execução bem-sucedida há mais de 2× o intervalo esperado
///
/// Cooldown de 15 minutos entre alertas do mesmo tipo via IDistributedCache.
/// Evita notificação storm em problemas prolongados.
/// </summary>
public sealed class PlatformHealthMonitorJob(
    IServiceScopeFactory serviceScopeFactory,
    IPlatformHealthReader healthReader,
    WorkerJobHealthRegistry jobHealthRegistry,
    IDistributedCache distributedCache,
    ILogger<PlatformHealthMonitorJob> logger) : BackgroundService
{
    internal const string HealthCheckName = "platform-health-monitor-job";

    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan StartDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan AlertCooldown = TimeSpan.FromMinutes(15);

    // Intervalo máximo esperado por job (2× o intervalo de execução configurado)
    private static readonly Dictionary<string, TimeSpan> JobStalenessThresholds = new()
    {
        [LicenseRecalculationJob.HealthCheckName] = TimeSpan.FromMinutes(30),
        [DriftDetectionJob.HealthCheckName] = TimeSpan.FromMinutes(20),
        [ContractConsumerIngestionJob.HealthCheckName] = TimeSpan.FromMinutes(60),
        [IncidentProbabilityRefreshJob.HealthCheckName] = TimeSpan.FromMinutes(60),
        [CloudBillingIngestionJob.HealthCheckName] = TimeSpan.FromMinutes(60),
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        jobHealthRegistry.MarkStarted(HealthCheckName);
        logger.LogInformation("PlatformHealthMonitorJob iniciado — intervalo {Interval}.", Interval);

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
                await RunChecksAsync(stoppingToken);
                jobHealthRegistry.MarkSucceeded(HealthCheckName);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                jobHealthRegistry.MarkFailed(HealthCheckName, "Ciclo de verificação de saúde falhou.");
                logger.LogError(ex, "Erro no ciclo do PlatformHealthMonitorJob.");
            }
        }

        logger.LogInformation("PlatformHealthMonitorJob parado.");
    }

    private async Task RunChecksAsync(CancellationToken cancellationToken)
    {
        await CheckOutboxBacklogAsync(cancellationToken);
        await CheckDiskUsageAsync(cancellationToken);
        await CheckStalledJobsAsync(cancellationToken);
    }

    private async Task CheckOutboxBacklogAsync(CancellationToken cancellationToken)
    {
        var pending = await healthReader.CountPendingOutboxAsync(cancellationToken);

        if (pending > 2000)
        {
            await SendAlertIfCooldownExpiredAsync(
                alertType: "outbox-critical",
                severity: "Critical",
                title: "Backlog crítico no Outbox da plataforma",
                message: $"Existem {pending} mensagens pendentes no Outbox. Risco de perda de eventos de integração.",
                cancellationToken);
        }
        else if (pending > 500)
        {
            await SendAlertIfCooldownExpiredAsync(
                alertType: "outbox-warning",
                severity: "Warning",
                title: "Backlog elevado no Outbox da plataforma",
                message: $"Existem {pending} mensagens pendentes no Outbox. Monitorizar a progressão.",
                cancellationToken);
        }
        else
        {
            logger.LogDebug("Outbox saudável: {Pending} mensagens pendentes.", pending);
        }
    }

    private async Task CheckDiskUsageAsync(CancellationToken cancellationToken)
    {
        var disk = healthReader.GetPrimaryDiskUsage();

        if (disk.TotalBytes == 0)
        {
            logger.LogDebug("Informação de disco não disponível — a ignorar verificação.");
            return;
        }

        var pct = disk.UsedPercent;

        if (pct >= 95)
        {
            await SendAlertIfCooldownExpiredAsync(
                alertType: "disk-critical",
                severity: "Critical",
                title: "Disco em estado crítico",
                message: $"O disco primário está {pct:F1}% preenchido ({disk.UsedBytes / 1_073_741_824L} GB de {disk.TotalBytes / 1_073_741_824L} GB). Ingestão pode ser afectada.",
                cancellationToken);
        }
        else if (pct >= 80)
        {
            await SendAlertIfCooldownExpiredAsync(
                alertType: "disk-warning",
                severity: "Warning",
                title: "Disco com uso elevado",
                message: $"O disco primário está {pct:F1}% preenchido ({disk.UsedBytes / 1_073_741_824L} GB de {disk.TotalBytes / 1_073_741_824L} GB).",
                cancellationToken);
        }
        else
        {
            logger.LogDebug("Disco saudável: {Percent:F1}% utilizado.", pct);
        }
    }

    private async Task CheckStalledJobsAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var (jobName, threshold) in JobStalenessThresholds)
        {
            var snapshot = jobHealthRegistry.GetSnapshot(jobName);

            if (snapshot is null)
                continue;

            var lastSuccess = snapshot.LastSuccessAt;

            if (lastSuccess is null || (now - lastSuccess.Value) > threshold)
            {
                var elapsed = lastSuccess is null
                    ? "nunca executou com sucesso"
                    : $"há {(now - lastSuccess.Value).TotalMinutes:F0} minutos";

                await SendAlertIfCooldownExpiredAsync(
                    alertType: $"stalled-job-{jobName}",
                    severity: "Warning",
                    title: $"Job '{jobName}' não executa há tempo excessivo",
                    message: $"O job '{jobName}' {elapsed}. Limite configurado: {threshold.TotalMinutes:F0} minutos.",
                    cancellationToken);
            }
        }
    }

    private async Task SendAlertIfCooldownExpiredAsync(
        string alertType,
        string severity,
        string title,
        string message,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"platform-alert:{alertType}";
        var existing = await distributedCache.GetStringAsync(cacheKey, cancellationToken);

        if (existing is not null)
        {
            logger.LogDebug("Alerta '{AlertType}' em cooldown — a ignorar.", alertType);
            return;
        }

        logger.LogWarning("[{Severity}] {Title}: {Message}", severity, title, message);

        await SetCooldownAsync(cacheKey, cancellationToken);

        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var notificationModule = scope.ServiceProvider.GetService<INotificationModule>();

            if (notificationModule is null)
            {
                logger.LogDebug("INotificationModule não disponível — alerta registado apenas em log.");
                return;
            }

            await notificationModule.SubmitAsync(new NotificationRequest
            {
                EventType = $"PlatformHealth.{alertType}",
                Category = "SystemAlert",
                Severity = severity,
                Title = title,
                Message = message,
                SourceModule = "Platform",
                SourceEntityType = "BackgroundJob",
                RecipientRoles = ["PlatformAdmin"],
                RequiresAction = severity == "Critical",
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao enviar alerta '{AlertType}' para INotificationModule.", alertType);
        }
    }

    private async Task SetCooldownAsync(string cacheKey, CancellationToken cancellationToken)
    {
        try
        {
            await distributedCache.SetStringAsync(
                cacheKey,
                "1",
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = AlertCooldown
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Não foi possível definir cooldown para '{CacheKey}'.", cacheKey);
        }
    }
}
