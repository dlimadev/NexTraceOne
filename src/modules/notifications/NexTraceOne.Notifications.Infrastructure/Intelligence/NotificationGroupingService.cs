using Microsoft.EntityFrameworkCore;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Infrastructure.Persistence;

namespace NexTraceOne.Notifications.Infrastructure.Intelligence;

/// <summary>
/// Implementação do serviço de agrupamento e correlação de notificações.
/// Gera chaves de correlação determinísticas e resolve grupos
/// com base em notificações existentes na mesma janela temporal.
/// </summary>
internal sealed class NotificationGroupingService(
    NotificationsDbContext context) : INotificationGroupingService
{
    /// <inheritdoc/>
    public string GenerateCorrelationKey(
        Guid tenantId,
        string eventType,
        string sourceModule,
        string? sourceEntityType,
        string? sourceEntityId)
    {
        // Chave de correlação: tenant + module + entityType + entityId
        // Notificações com mesma chave são consideradas correlatas
        var parts = new List<string>
        {
            tenantId.ToString("N"),
            sourceModule,
            eventType
        };

        if (!string.IsNullOrWhiteSpace(sourceEntityType))
            parts.Add(sourceEntityType);

        if (!string.IsNullOrWhiteSpace(sourceEntityId))
            parts.Add(sourceEntityId);

        return string.Join("|", parts);
    }

    /// <inheritdoc/>
    public async Task<Guid?> ResolveGroupAsync(
        Guid tenantId,
        string correlationKey,
        int windowMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(correlationKey) || windowMinutes <= 0)
            return null;

        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-windowMinutes);

        // Procurar grupo existente com a mesma chave de correlação
        var existingGroupId = await context.Notifications
            .Where(n => n.TenantId == tenantId
                     && n.CorrelationKey == correlationKey
                     && n.GroupId != null
                     && n.CreatedAt >= cutoff
                     && n.Status != NotificationStatus.Dismissed
                     && n.Status != NotificationStatus.Archived)
            .Select(n => n.GroupId)
            .FirstOrDefaultAsync(cancellationToken);

        // Se já existe grupo, reutilizar; senão gerar novo
        return existingGroupId ?? Guid.NewGuid();
    }
}
