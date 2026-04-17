using Microsoft.EntityFrameworkCore;

using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;
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
/// A janela e o estado do feature são lidos de <c>notifications.dedup.*</c> via
/// <see cref="IConfigurationResolutionService"/>; caso a chave não exista usa-se
/// o valor do parâmetro <paramref name="windowMinutes"/> como fallback.
/// </summary>
internal sealed class NotificationDeduplicationService(
    NotificationsDbContext context,
    IConfigurationResolutionService configResolution) : INotificationDeduplicationService
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
        // Verificar se a deduplicação está habilitada (default: habilitada)
        var enabledDto = await configResolution.ResolveEffectiveValueAsync(
            "notifications.dedup.enabled",
            ConfigurationScope.Tenant,
            tenantId.ToString(),
            cancellationToken);

        if (enabledDto is not null
            && bool.TryParse(enabledDto.EffectiveValue, out var enabled)
            && !enabled)
            return false;

        // Ler janela configurada; fallback para o parâmetro recebido
        var windowDto = await configResolution.ResolveEffectiveValueAsync(
            "notifications.dedup.window_minutes",
            ConfigurationScope.Tenant,
            tenantId.ToString(),
            cancellationToken);

        if (windowDto is not null && int.TryParse(windowDto.EffectiveValue, out var configuredWindow) && configuredWindow > 0)
            windowMinutes = configuredWindow;

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
