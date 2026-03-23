using Microsoft.EntityFrameworkCore;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Infrastructure.Persistence;

namespace NexTraceOne.Notifications.Infrastructure.Engine;

/// <summary>
/// Implementação de deduplicação básica com acesso ao banco de dados.
/// Verifica se já existe uma notificação recente com a mesma combinação de:
/// tenant + destinatário + tipo de evento + entidade de origem,
/// dentro de uma janela temporal configurável.
/// Evita spam óbvio e duplicação por reprocessamento de eventos.
/// </summary>
internal sealed class NotificationDeduplicationService(
    NotificationsDbContext context) : INotificationDeduplicationService
{
    /// <inheritdoc/>
    public async Task<bool> IsDuplicateAsync(
        Guid tenantId,
        Guid recipientUserId,
        string eventType,
        string? sourceEntityId,
        int windowMinutes = 5,
        CancellationToken cancellationToken = default)
    {
        if (windowMinutes <= 0)
            return false;

        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-windowMinutes);

        var query = context.Notifications
            .Where(n => n.TenantId == tenantId
                     && n.RecipientUserId == recipientUserId
                     && n.EventType == eventType
                     && n.CreatedAt >= cutoff
                     && n.Status != NotificationStatus.Dismissed
                     && n.Status != NotificationStatus.Archived);

        if (!string.IsNullOrWhiteSpace(sourceEntityId))
        {
            query = query.Where(n => n.SourceEntityId == sourceEntityId);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
