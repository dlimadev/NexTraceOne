using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.ExternalDelivery;

namespace NexTraceOne.Notifications.Infrastructure.ExternalDelivery;

/// <summary>
/// Job de background que processa retries de entrega de notificações agendados.
/// Executa periodicamente a cada <see cref="DeliveryRetryOptions.RetryJobIntervalSeconds"/> segundos,
/// busca registos de NotificationDelivery com Status=RetryScheduled e NextRetryAt ≤ agora,
/// e re-despacha a entrega via IExternalDeliveryService.
///
/// Design:
/// - Usa PeriodicTimer (compatível com .NET 6+, sem Quartz) para ciclos periódicos.
/// - Cria um scope por ciclo para isolar DbContext e serviços Scoped.
/// - Erros individuais por delivery não interrompem o ciclo completo.
/// - Configura-se via DeliveryRetryOptions (appsettings: Notifications:Retry).
/// </summary>
internal sealed class NotificationDeliveryRetryJob(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<NotificationDeliveryRetryJob> logger) : BackgroundService
{
    internal const string HealthCheckName = "notification-delivery-retry";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("NotificationDeliveryRetryJob started.");

        // Aguardar antes do primeiro ciclo para deixar a aplicação inicializar
        using var initScope = serviceScopeFactory.CreateScope();
        var initOpts = initScope.ServiceProvider.GetRequiredService<IOptions<DeliveryRetryOptions>>().Value;
        await Task.Delay(TimeSpan.FromSeconds(initOpts.RetryJobStartupDelaySeconds), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            int intervalSeconds;
            try
            {
                await RunRetryCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in NotificationDeliveryRetryJob cycle.");
            }

            // Ler o intervalo numa scope temporária para suportar reconfiguração em runtime
            using var scope = serviceScopeFactory.CreateScope();
            var opts = scope.ServiceProvider.GetRequiredService<IOptions<DeliveryRetryOptions>>().Value;
            intervalSeconds = opts.RetryJobIntervalSeconds;

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }

        logger.LogInformation("NotificationDeliveryRetryJob stopped.");
    }

    private async Task RunRetryCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var deliveryStore = scope.ServiceProvider.GetRequiredService<INotificationDeliveryStore>();
        var notificationStore = scope.ServiceProvider.GetRequiredService<INotificationStore>();
        var externalDelivery = scope.ServiceProvider.GetRequiredService<IExternalDeliveryService>();
        var opts = scope.ServiceProvider.GetRequiredService<IOptions<DeliveryRetryOptions>>().Value;

        var now = DateTimeOffset.UtcNow;
        var scheduled = await deliveryStore.ListScheduledForRetryAsync(
            now,
            opts.MaxAttempts,
            batchSize: opts.RetryJobBatchSize,
            cancellationToken);

        if (scheduled.Count == 0)
            return;

        logger.LogInformation(
            "NotificationDeliveryRetryJob: processing {Count} scheduled retries.",
            scheduled.Count);

        var processed = 0;
        var failed = 0;

        foreach (var delivery in scheduled)
        {
            try
            {
                var notification = await notificationStore.GetByIdAsync(delivery.NotificationId, cancellationToken);
                if (notification is null)
                {
                    logger.LogWarning(
                        "Notification {NotificationId} not found for delivery {DeliveryId}. Marking as failed.",
                        delivery.NotificationId.Value, delivery.Id.Value);
                    delivery.MarkFailed("Notification record not found.");
                    await deliveryStore.SaveChangesAsync(cancellationToken);
                    failed++;
                    continue;
                }

                await externalDelivery.RetryDeliveryAsync(delivery, notification, cancellationToken);
                processed++;

                logger.LogDebug(
                    "Retry processed: delivery={DeliveryId}, channel={Channel}, status={Status}",
                    delivery.Id.Value, delivery.Channel, delivery.Status);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to process retry for delivery {DeliveryId}.",
                    delivery.Id.Value);
                failed++;
            }
        }

        if (processed > 0 || failed > 0)
        {
            logger.LogInformation(
                "NotificationDeliveryRetryJob cycle complete: {Processed} retried, {Failed} failed.",
                processed, failed);
        }
    }
}
