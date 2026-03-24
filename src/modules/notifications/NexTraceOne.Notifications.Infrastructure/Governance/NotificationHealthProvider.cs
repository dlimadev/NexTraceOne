using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Infrastructure.Persistence;

namespace NexTraceOne.Notifications.Infrastructure.Governance;

/// <summary>
/// Implementação do health provider da plataforma de notificações.
/// Phase 7 — verifica estado de cada componente: store, canais, delivery backlog.
///
/// Componentes verificados:
///   - InAppStore: se o DbContext responde
///   - DeliveryPipeline: se há backlog excessivo de deliveries pendentes
///   - EmailChannel: se há falhas recentes no canal email
///   - TeamsChannel: se há falhas recentes no canal Teams
/// </summary>
internal sealed class NotificationHealthProvider(
    NotificationsDbContext context,
    ILogger<NotificationHealthProvider> logger) : INotificationHealthProvider
{
    private const int MaxPendingBacklog = 100;
    private const int RecentFailureWindowMinutes = 60;
    private const int MaxRecentFailures = 10;

    /// <inheritdoc/>
    public async Task<NotificationHealthReport> GetHealthAsync(
        CancellationToken cancellationToken = default)
    {
        var components = new List<NotificationComponentHealth>();

        // 1. InApp Store health
        components.Add(await CheckStoreHealthAsync(cancellationToken));

        // 2. Delivery pipeline health
        components.Add(await CheckDeliveryPipelineAsync(cancellationToken));

        // 3. Email channel health
        components.Add(await CheckChannelHealthAsync(DeliveryChannel.Email, "EmailChannel", cancellationToken));

        // 4. Teams channel health
        components.Add(await CheckChannelHealthAsync(DeliveryChannel.MicrosoftTeams, "TeamsChannel", cancellationToken));

        var overallStatus = components.Any(c => c.Status == NotificationHealthStatus.Unhealthy)
            ? NotificationHealthStatus.Unhealthy
            : components.Any(c => c.Status == NotificationHealthStatus.Degraded)
                ? NotificationHealthStatus.Degraded
                : NotificationHealthStatus.Healthy;

        logger.LogDebug("Notification health check completed: {Status}", overallStatus);

        return new NotificationHealthReport
        {
            OverallStatus = overallStatus,
            Components = components
        };
    }

    private async Task<NotificationComponentHealth> CheckStoreHealthAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var canConnect = await context.Database.CanConnectAsync(cancellationToken);
            return new NotificationComponentHealth
            {
                Name = "InAppStore",
                Status = canConnect ? NotificationHealthStatus.Healthy : NotificationHealthStatus.Unhealthy,
                Description = canConnect ? "Database connection OK" : "Cannot connect to database"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Notification store health check failed");
            return new NotificationComponentHealth
            {
                Name = "InAppStore",
                Status = NotificationHealthStatus.Unhealthy,
                Description = "Health check failed: " + ex.Message
            };
        }
    }

    private async Task<NotificationComponentHealth> CheckDeliveryPipelineAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var pendingCount = await context.Deliveries
                .CountAsync(d => d.Status == DeliveryStatus.Pending, cancellationToken);

            var status = pendingCount > MaxPendingBacklog
                ? NotificationHealthStatus.Degraded
                : NotificationHealthStatus.Healthy;

            return new NotificationComponentHealth
            {
                Name = "DeliveryPipeline",
                Status = status,
                Description = $"Pending deliveries: {pendingCount}",
                Metadata = new Dictionary<string, string>
                {
                    ["PendingCount"] = pendingCount.ToString(),
                    ["MaxBacklog"] = MaxPendingBacklog.ToString()
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Delivery pipeline health check failed");
            return new NotificationComponentHealth
            {
                Name = "DeliveryPipeline",
                Status = NotificationHealthStatus.Unhealthy,
                Description = "Health check failed: " + ex.Message
            };
        }
    }

    private async Task<NotificationComponentHealth> CheckChannelHealthAsync(
        DeliveryChannel channel,
        string componentName,
        CancellationToken cancellationToken)
    {
        try
        {
            var cutoff = DateTimeOffset.UtcNow.AddMinutes(-RecentFailureWindowMinutes);
            var recentFailures = await context.Deliveries
                .CountAsync(d => d.Channel == channel
                              && d.Status == DeliveryStatus.Failed
                              && d.FailedAt >= cutoff,
                    cancellationToken);

            var status = recentFailures >= MaxRecentFailures
                ? NotificationHealthStatus.Degraded
                : NotificationHealthStatus.Healthy;

            return new NotificationComponentHealth
            {
                Name = componentName,
                Status = status,
                Description = recentFailures > 0
                    ? $"{recentFailures} failure(s) in the last {RecentFailureWindowMinutes} minutes"
                    : "No recent failures",
                Metadata = new Dictionary<string, string>
                {
                    ["RecentFailures"] = recentFailures.ToString(),
                    ["WindowMinutes"] = RecentFailureWindowMinutes.ToString()
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Channel {Channel} health check failed", componentName);
            return new NotificationComponentHealth
            {
                Name = componentName,
                Status = NotificationHealthStatus.Unhealthy,
                Description = "Health check failed: " + ex.Message
            };
        }
    }
}
