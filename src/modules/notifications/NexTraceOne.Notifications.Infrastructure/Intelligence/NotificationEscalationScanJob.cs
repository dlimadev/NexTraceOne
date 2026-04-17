using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Infrastructure.Intelligence;

/// <summary>
/// Job de background que varre periodicamente notificações críticas e ActionRequired
/// não tratadas, e invoca o serviço de escalação quando os thresholds são excedidos.
///
/// Os thresholds de escalação são lidos de <c>notifications.escalation.*</c> via
/// <see cref="IConfigurationResolutionService"/> em cada ciclo, permitindo ajuste
/// sem redeploy. Caso a feature <c>notifications.escalation.enabled</c> esteja
/// desactivada, o ciclo é ignorado completamente.
///
/// Design:
///   - Executa a cada 5 minutos via PeriodicTimer.
///   - Cria um scope por ciclo para isolar DbContext e serviços Scoped.
///   - Falhas num batch não interrompem ciclos seguintes.
///   - Idempotente: <see cref="INotificationEscalationService.ShouldEscalate"/> ignora já escaladas.
/// </summary>
internal sealed class NotificationEscalationScanJob(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<NotificationEscalationScanJob> logger) : BackgroundService
{
    private static readonly TimeSpan ScanInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(30);
    private const int BatchSize = 200;
    private const int DefaultCriticalThresholdMinutes = 30;
    private const int DefaultActionRequiredThresholdMinutes = 120;

    internal const string HealthCheckName = "notification-escalation-scan";

    private static readonly IReadOnlyList<NotificationSeverity> EscalationTargetSeverities =
        [NotificationSeverity.Critical, NotificationSeverity.ActionRequired];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("NotificationEscalationScanJob started.");

        try
        {
            await Task.Delay(StartupDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(ScanInterval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunEscalationCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in NotificationEscalationScanJob cycle.");
            }
        }

        logger.LogInformation("NotificationEscalationScanJob stopped.");
    }

    private async Task RunEscalationCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<INotificationStore>();
        var escalationService = scope.ServiceProvider.GetRequiredService<INotificationEscalationService>();
        var configResolution = scope.ServiceProvider.GetRequiredService<IConfigurationResolutionService>();

        // Ler estado do feature e thresholds da configuração
        var escalationEnabledDto = await configResolution.ResolveEffectiveValueAsync(
            "notifications.escalation.enabled",
            ConfigurationScope.System,
            null,
            cancellationToken);

        if (escalationEnabledDto is not null
            && bool.TryParse(escalationEnabledDto.EffectiveValue, out var escalationEnabled)
            && !escalationEnabled)
        {
            logger.LogDebug("Escalation scan skipped: notifications.escalation.enabled is false.");
            return;
        }

        var criticalMinutes = DefaultCriticalThresholdMinutes;
        var criticalDto = await configResolution.ResolveEffectiveValueAsync(
            "notifications.escalation.critical_threshold_minutes",
            ConfigurationScope.System,
            null,
            cancellationToken);
        if (criticalDto is not null
            && int.TryParse(criticalDto.EffectiveValue, out var cm)
            && cm > 0)
            criticalMinutes = cm;

        var actionRequiredMinutes = DefaultActionRequiredThresholdMinutes;
        var arDto = await configResolution.ResolveEffectiveValueAsync(
            "notifications.escalation.action_required_threshold_minutes",
            ConfigurationScope.System,
            null,
            cancellationToken);
        if (arDto is not null
            && int.TryParse(arDto.EffectiveValue, out var arm)
            && arm > 0)
            actionRequiredMinutes = arm;

        // Varrer com o threshold mais baixo (Critical)
        var olderThan = DateTimeOffset.UtcNow.AddMinutes(-criticalMinutes);
        var skip = 0;
        var totalEscalated = 0;

        while (true)
        {
            // Usar Guid.Empty para TenantId = varrer cross-tenant
            // Nota: o store usa TenantId via RLS; para o background job que precisa de varrer
            // todos os tenants, a query sem filtro de TenantId é necessária.
            // Como o job usa WorkerCurrentTenant (sem tenant definido), o RLS está desativado.
            var batch = await store.ListForEscalationAsync(
                tenantId: Guid.Empty, // Job executa sem tenant: RLS desativado no BackgroundWorkers
                olderThan: olderThan,
                severities: EscalationTargetSeverities,
                skip: skip,
                take: BatchSize,
                cancellationToken);

            if (batch.Count == 0)
                break;

            var escalatedInBatch = 0;
            foreach (var notification in batch)
            {
                if (!escalationService.ShouldEscalate(notification, criticalMinutes, actionRequiredMinutes))
                    continue;

                try
                {
                    await escalationService.EscalateAsync(notification, cancellationToken);
                    escalatedInBatch++;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        ex,
                        "Escalation failed for notification {NotificationId}. Skipping.",
                        notification.Id.Value);
                }
            }

            if (escalatedInBatch > 0)
                await store.SaveChangesAsync(cancellationToken);

            totalEscalated += escalatedInBatch;
            skip += batch.Count;

            if (batch.Count < BatchSize)
                break;
        }

        if (totalEscalated > 0)
        {
            logger.LogInformation(
                "Escalation scan completed: {Count} notification(s) escalated.",
                totalEscalated);
        }
    }
}
