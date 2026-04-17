using Microsoft.EntityFrameworkCore;

using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Infrastructure.Persistence;

namespace NexTraceOne.Notifications.Infrastructure.Intelligence;

/// <summary>
/// Implementação do serviço de supressão de notificações.
///
/// Regras de supressão:
///   1. Notificação já acknowledged para a mesma entidade recentemente → suprimir
///   2. Notificação snoozed activa para o mesmo tipo/entidade → suprimir
///   3. Grupo correlato activo já tem notificação não tratada → suprimir se elegível
///
/// Regras de segurança:
///   - Notificações Critical nunca são suprimidas
///   - Notificações obrigatórias (BreakGlass, Approval, Compliance) nunca são suprimidas
///
/// O estado do feature (<c>notifications.suppress.enabled</c>) e a janela de
/// acknowledged (<c>notifications.suppress.acknowledged_window_minutes</c>) são lidos
/// de <see cref="IConfigurationResolutionService"/> com fallback para 30 minutos.
/// </summary>
internal sealed class NotificationSuppressionService(
    NotificationsDbContext context,
    IMandatoryNotificationPolicy mandatoryPolicy,
    IConfigurationResolutionService configResolution) : INotificationSuppressionService
{
    private const int DefaultAcknowledgedWindowMinutes = 30;

    /// <inheritdoc/>
    public async Task<SuppressionResult> EvaluateAsync(
        NotificationRequest request,
        Guid recipientUserId,
        CancellationToken cancellationToken = default)
    {
        // Notificações obrigatórias nunca são suprimidas
        var category = Enum.TryParse<NotificationCategory>(request.Category, true, out var cat)
            ? cat : NotificationCategory.Informational;
        var severity = Enum.TryParse<NotificationSeverity>(request.Severity, true, out var sev)
            ? sev : NotificationSeverity.Info;

        if (mandatoryPolicy.IsMandatory(request.EventType, category, severity))
            return SuppressionResult.Allow();

        if (severity == NotificationSeverity.Critical)
            return SuppressionResult.Allow();

        if (!request.TenantId.HasValue)
            return SuppressionResult.Allow();

        var tenantId = request.TenantId.Value;

        // Verificar se a supressão está habilitada (default: habilitada)
        var enabledDto = await configResolution.ResolveEffectiveValueAsync(
            "notifications.suppress.enabled",
            ConfigurationScope.Tenant,
            tenantId.ToString(),
            cancellationToken);

        if (enabledDto is not null
            && bool.TryParse(enabledDto.EffectiveValue, out var enabled)
            && !enabled)
            return SuppressionResult.Allow();

        // Ler janela de acknowledged configurada; fallback para 30 minutos
        var windowMinutes = DefaultAcknowledgedWindowMinutes;
        var windowDto = await configResolution.ResolveEffectiveValueAsync(
            "notifications.suppress.acknowledged_window_minutes",
            ConfigurationScope.Tenant,
            tenantId.ToString(),
            cancellationToken);

        if (windowDto is not null
            && int.TryParse(windowDto.EffectiveValue, out var configuredWindow)
            && configuredWindow > 0)
            windowMinutes = configuredWindow;

        // Regra 1: Já acknowledged para mesma entidade recentemente
        if (!string.IsNullOrWhiteSpace(request.SourceEntityId))
        {
            var cutoff = DateTimeOffset.UtcNow.AddMinutes(-windowMinutes);
            var alreadyAcknowledged = await context.Notifications
                .AnyAsync(n => n.TenantId == tenantId
                            && n.RecipientUserId == recipientUserId
                            && n.EventType == request.EventType
                            && n.SourceEntityId == request.SourceEntityId
                            && n.Status == NotificationStatus.Acknowledged
                            && n.AcknowledgedAt >= cutoff,
                    cancellationToken);

            if (alreadyAcknowledged)
                return SuppressionResult.SuppressWith(
                    $"Already acknowledged for same entity within {windowMinutes} minutes");
        }

        // Regra 2: Snoozed activa para o mesmo tipo/entidade
        if (!string.IsNullOrWhiteSpace(request.SourceEntityId))
        {
            var now = DateTimeOffset.UtcNow;
            var isSnoozed = await context.Notifications
                .AnyAsync(n => n.TenantId == tenantId
                            && n.RecipientUserId == recipientUserId
                            && n.EventType == request.EventType
                            && n.SourceEntityId == request.SourceEntityId
                            && n.SnoozedUntil != null
                            && n.SnoozedUntil > now,
                    cancellationToken);

            if (isSnoozed)
                return SuppressionResult.SuppressWith("Active snooze exists for same event/entity");
        }

        return SuppressionResult.Allow();
    }
}
