using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NexTraceOne.Notifications.Application.Abstractions;

namespace NexTraceOne.Notifications.Infrastructure.Intelligence;

/// <summary>
/// Job de background que gera um digest diário de notificações acumuladas
/// para utilizadores com notificações não tratadas elegíveis.
///
/// Comportamento:
///   - Executa uma vez por dia (cada 24 horas).
///   - Gera digest para todas as notificações Info/ActionRequired não tratadas nas últimas 24h.
///   - Notificações Critical e Warning são excluídas do digest (são entregues em tempo real).
///   - Delega a lógica de geração ao INotificationDigestService.
///   - Regista o resumo no log para rastreabilidade operacional.
///
/// Entrega futura:
///   - A entrega do digest por email/Teams deve ser feita aqui
///     quando IExternalDeliveryService suportar notificações do tipo Digest.
///
/// Design:
///   - Usa PeriodicTimer com intervalo de 24h.
///   - Cria scope por ciclo para isolar DbContext.
///   - Falhas por utilizador não interrompem os demais.
/// </summary>
internal sealed class NotificationDigestJob(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<NotificationDigestJob> logger) : BackgroundService
{
    private static readonly TimeSpan DigestInterval = TimeSpan.FromHours(24);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan DigestWindowHours = TimeSpan.FromHours(24);
    private const int BatchSize = 500;

    internal const string HealthCheckName = "notification-digest";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("NotificationDigestJob started.");

        try
        {
            await Task.Delay(StartupDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(DigestInterval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunDigestCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in NotificationDigestJob cycle.");
            }
        }

        logger.LogInformation("NotificationDigestJob stopped.");
    }

    private async Task RunDigestCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<INotificationStore>();
        var digestService = scope.ServiceProvider.GetRequiredService<INotificationDigestService>();

        var since = DateTimeOffset.UtcNow.Subtract(DigestWindowHours);

        // Buscar notificações elegíveis para digest (cross-tenant no BackgroundWorkers)
        var notifications = await store.ListForDigestAsync(
            tenantId: Guid.Empty, // Job executa sem tenant: RLS desativado no BackgroundWorkers
            since: since,
            skip: 0,
            take: BatchSize,
            cancellationToken);

        if (notifications.Count == 0)
        {
            logger.LogDebug("No eligible notifications for digest in the last {Window}h.", DigestWindowHours.TotalHours);
            return;
        }

        // Agrupar por tenantId + userId para gerar digest por utilizador
        var byUser = notifications
            .GroupBy(n => (n.TenantId, n.RecipientUserId))
            .ToList();

        var totalGenerated = 0;

        foreach (var group in byUser)
        {
            var (tenantId, userId) = group.Key;

            try
            {
                var result = await digestService.GenerateDigestAsync(userId, tenantId, cancellationToken);

                if (result.Generated && result.NotificationCount > 0)
                {
                    totalGenerated++;
                    logger.LogDebug(
                        "Digest generated for user {UserId} in tenant {TenantId}: {Count} notification(s).",
                        userId, tenantId, result.NotificationCount);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Digest generation failed for user {UserId} in tenant {TenantId}. Skipping.",
                    userId, tenantId);
            }
        }

        if (totalGenerated > 0)
        {
            logger.LogInformation(
                "Digest cycle completed: digests generated for {Count} user(s).",
                totalGenerated);
        }
    }
}
