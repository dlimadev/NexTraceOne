using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Infrastructure.Persistence;

namespace NexTraceOne.Notifications.Infrastructure.Governance;

/// <summary>
/// Implementação do serviço de métricas da plataforma de notificações.
/// Phase 7 — consulta dados agregados da central e delivery log para fornecer
/// métricas operacionais, de interação e de qualidade.
/// </summary>
internal sealed class NotificationMetricsService(
    NotificationsDbContext context,
    ILogger<NotificationMetricsService> logger) : INotificationMetricsService
{
    /// <inheritdoc/>
    public async Task<NotificationPlatformMetrics> GetPlatformMetricsAsync(
        Guid tenantId,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default)
    {
        var notifications = context.Notifications
            .Where(n => n.TenantId == tenantId && n.CreatedAt >= from && n.CreatedAt <= until);

        var totalGenerated = await notifications.CountAsync(cancellationToken);

        var byCategory = await notifications
            .GroupBy(n => n.Category)
            .Select(g => new { Category = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Category, x => x.Count, cancellationToken);

        var bySeverity = await notifications
            .GroupBy(n => n.Severity)
            .Select(g => new { Severity = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Severity, x => x.Count, cancellationToken);

        var bySourceModule = await notifications
            .GroupBy(n => n.SourceModule)
            .Select(g => new { Module = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Module, x => x.Count, cancellationToken);

        // Delivery metrics
        var deliveries = context.Deliveries
            .Where(d => d.CreatedAt >= from && d.CreatedAt <= until);

        var deliveriesByChannel = await deliveries
            .GroupBy(d => d.Channel)
            .Select(g => new { Channel = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Channel, x => x.Count, cancellationToken);

        var totalDelivered = await deliveries.CountAsync(d => d.Status == DeliveryStatus.Delivered, cancellationToken);
        var totalFailed = await deliveries.CountAsync(d => d.Status == DeliveryStatus.Failed, cancellationToken);
        var totalPending = await deliveries.CountAsync(d => d.Status == DeliveryStatus.Pending, cancellationToken);
        var totalSkipped = await deliveries.CountAsync(d => d.Status == DeliveryStatus.Skipped, cancellationToken);

        logger.LogDebug(
            "Platform metrics generated for tenant {TenantId}: {Total} notifications, {Delivered} delivered, {Failed} failed",
            tenantId, totalGenerated, totalDelivered, totalFailed);

        return new NotificationPlatformMetrics
        {
            TotalGenerated = totalGenerated,
            ByCategory = byCategory,
            BySeverity = bySeverity,
            BySourceModule = bySourceModule,
            DeliveriesByChannel = deliveriesByChannel,
            TotalDelivered = totalDelivered,
            TotalFailed = totalFailed,
            TotalPending = totalPending,
            TotalSkipped = totalSkipped
        };
    }

    /// <inheritdoc/>
    public async Task<NotificationInteractionMetrics> GetInteractionMetricsAsync(
        Guid tenantId,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default)
    {
        var notifications = context.Notifications
            .Where(n => n.TenantId == tenantId && n.CreatedAt >= from && n.CreatedAt <= until);

        var total = await notifications.CountAsync(cancellationToken);

        var totalRead = await notifications.CountAsync(
            n => n.Status == NotificationStatus.Read
              || n.Status == NotificationStatus.Acknowledged
              || n.Status == NotificationStatus.Archived, cancellationToken);

        var totalUnread = await notifications.CountAsync(
            n => n.Status == NotificationStatus.Unread, cancellationToken);

        var totalAcknowledged = await notifications.CountAsync(
            n => n.Status == NotificationStatus.Acknowledged, cancellationToken);

        var totalSnoozed = await notifications.CountAsync(
            n => n.SnoozedUntil != null, cancellationToken);

        var totalArchived = await notifications.CountAsync(
            n => n.Status == NotificationStatus.Archived, cancellationToken);

        var totalDismissed = await notifications.CountAsync(
            n => n.Status == NotificationStatus.Dismissed, cancellationToken);

        var totalEscalated = await notifications.CountAsync(
            n => n.IsEscalated, cancellationToken);

        var readRate = total > 0 ? (decimal)totalRead / total : 0m;

        var totalRequiringAction = await notifications.CountAsync(
            n => n.RequiresAction, cancellationToken);
        var ackRate = totalRequiringAction > 0
            ? (decimal)totalAcknowledged / totalRequiringAction
            : 0m;

        var totalUnacknowledgedActionRequired = await notifications.CountAsync(
            n => n.RequiresAction && n.AcknowledgedAt == null,
            cancellationToken);

        var readDurations = await notifications
            .Where(n => n.ReadAt != null)
            .Select(n => new
            {
                n.CreatedAt,
                ReadAt = n.ReadAt!.Value
            })
            .ToListAsync(cancellationToken);

        var acknowledgeDurations = await notifications
            .Where(n => n.RequiresAction && n.AcknowledgedAt != null)
            .Select(n => new
            {
                n.CreatedAt,
                AcknowledgedAt = n.AcknowledgedAt!.Value
            })
            .ToListAsync(cancellationToken);

        var averageTimeToReadMinutes = readDurations.Count > 0
            ? readDurations.Average(item => (decimal)(item.ReadAt - item.CreatedAt).TotalMinutes)
            : 0m;

        var averageTimeToAcknowledgeMinutes = acknowledgeDurations.Count > 0
            ? acknowledgeDurations.Average(item => (decimal)(item.AcknowledgedAt - item.CreatedAt).TotalMinutes)
            : 0m;

        return new NotificationInteractionMetrics
        {
            TotalRead = totalRead,
            TotalUnread = totalUnread,
            TotalAcknowledged = totalAcknowledged,
            TotalSnoozed = totalSnoozed,
            TotalArchived = totalArchived,
            TotalDismissed = totalDismissed,
            TotalEscalated = totalEscalated,
            ReadRate = Math.Round(readRate, 4),
            AcknowledgeRate = Math.Round(ackRate, 4),
            AverageTimeToReadMinutes = Math.Round(averageTimeToReadMinutes, 2),
            AverageTimeToAcknowledgeMinutes = Math.Round(averageTimeToAcknowledgeMinutes, 2),
            TotalUnacknowledgedActionRequired = totalUnacknowledgedActionRequired
        };
    }

    /// <inheritdoc/>
    public async Task<NotificationQualityMetrics> GetQualityMetricsAsync(
        Guid tenantId,
        DateTimeOffset from,
        DateTimeOffset until,
        CancellationToken cancellationToken = default)
    {
        var notifications = context.Notifications
            .Where(n => n.TenantId == tenantId && n.CreatedAt >= from && n.CreatedAt <= until);

        var totalDays = Math.Max(1, (until - from).TotalDays);
        var totalNotifications = await notifications.CountAsync(cancellationToken);

        var distinctUsers = await notifications
            .Select(n => n.RecipientUserId)
            .Distinct()
            .CountAsync(cancellationToken);

        var avgPerUserPerDay = distinctUsers > 0
            ? (decimal)totalNotifications / distinctUsers / (decimal)totalDays
            : 0m;

        var totalSuppressed = await notifications.CountAsync(n => n.IsSuppressed, cancellationToken);
        var totalGrouped = await notifications.CountAsync(n => n.GroupId != null, cancellationToken);
        var totalCorrelated = await notifications.CountAsync(n => n.CorrelatedIncidentId != null, cancellationToken);

        var topNoisyTypes = await notifications
            .GroupBy(n => n.EventType)
            .Select(g => new { EventType = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync(cancellationToken);

        // Least engaged: types with lowest read rate
        var leastEngaged = await notifications
            .GroupBy(n => n.EventType)
            .Select(g => new
            {
                EventType = g.Key,
                Total = g.Count(),
                ReadCount = g.Count(n => n.Status != NotificationStatus.Unread)
            })
            .Where(x => x.Total >= 3) // Minimum sample for relevance
            .OrderBy(x => (decimal)x.ReadCount / x.Total)
            .Take(5)
            .ToListAsync(cancellationToken);

        var unacknowledgedActionTypes = await notifications
            .Where(n => n.RequiresAction && n.AcknowledgedAt == null)
            .GroupBy(n => n.EventType)
            .Select(g => new { EventType = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync(cancellationToken);

        return new NotificationQualityMetrics
        {
            AveragePerUserPerDay = Math.Round(avgPerUserPerDay, 2),
            TotalSuppressed = totalSuppressed,
            TotalGrouped = totalGrouped,
            TotalCorrelatedWithIncidents = totalCorrelated,
            TopNoisyTypes = topNoisyTypes.Select(t => new NotificationTypeCount(t.EventType, t.Count)).ToList(),
            LeastEngagedTypes = leastEngaged.Select(t => new NotificationTypeCount(t.EventType, t.Total)).ToList(),
            UnacknowledgedActionTypes = unacknowledgedActionTypes.Select(t => new NotificationTypeCount(t.EventType, t.Count)).ToList()
        };
    }
}
