using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Infrastructure.Persistence;

namespace NexTraceOne.Notifications.Infrastructure.Intelligence;

/// <summary>
/// Implementação do serviço de digest de notificações.
/// Gera resumos consolidados de notificações de baixa/média urgência
/// acumuladas para um utilizador num período.
///
/// Regras:
///   - Apenas notificações Info e ActionRequired são elegíveis
///   - Notificações Critical e Warning são excluídas do digest
///   - Notificações já lidas, acknowledged ou arquivadas são excluídas
///   - O digest agrupa por categoria e gera contagem
/// </summary>
internal sealed class NotificationDigestService(
    NotificationsDbContext context,
    ILogger<NotificationDigestService> logger) : INotificationDigestService
{
    private const int DigestWindowHours = 24;

    /// <inheritdoc/>
    public async Task<DigestResult> GenerateDigestAsync(
        Guid recipientUserId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow.AddHours(-DigestWindowHours);

        // Buscar notificações elegíveis para digest (Info/ActionRequired, não lidas/não tratadas)
        var eligibleNotifications = await context.Notifications
            .Where(n => n.TenantId == tenantId
                     && n.RecipientUserId == recipientUserId
                     && n.CreatedAt >= cutoff
                     && n.Status == NotificationStatus.Unread
                     && !n.IsSuppressed
                     && (n.Severity == NotificationSeverity.Info || n.Severity == NotificationSeverity.ActionRequired))
            .GroupBy(n => n.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        if (eligibleNotifications.Count == 0)
            return new DigestResult(false, 0);

        var totalCount = eligibleNotifications.Sum(g => g.Count);
        var summaryParts = eligibleNotifications
            .OrderByDescending(g => g.Count)
            .Select(g => $"{g.Category}: {g.Count}");
        var summary = $"You have {totalCount} pending notification(s): {string.Join(", ", summaryParts)}";

        logger.LogInformation(
            "Digest generated for user {UserId} in tenant {TenantId}: {Count} notifications across {Categories} categories",
            recipientUserId, tenantId, totalCount, eligibleNotifications.Count);

        return new DigestResult(true, totalCount, summary);
    }
}
