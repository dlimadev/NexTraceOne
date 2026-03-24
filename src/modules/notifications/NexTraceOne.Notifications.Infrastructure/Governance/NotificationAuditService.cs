using Microsoft.Extensions.Logging;

using NexTraceOne.Notifications.Application.Abstractions;

namespace NexTraceOne.Notifications.Infrastructure.Governance;

/// <summary>
/// Implementação do serviço de auditoria de notificações.
/// Phase 7 — regista eventos auditáveis na plataforma de notificações
/// utilizando logging estruturado para integração com o audit trail do NexTraceOne.
///
/// Eventos auditados:
///   - Notificações críticas geradas/entregues/falhadas
///   - Acknowledge de notificações críticas
///   - Snooze de notificações
///   - Escalation disparado
///   - Incidente criado a partir de notificação
///   - Mudanças de preferências
///   - Suppression aplicada
/// </summary>
internal sealed class NotificationAuditService(
    ILogger<NotificationAuditService> logger) : INotificationAuditService
{
    /// <inheritdoc/>
    public Task RecordAsync(
        NotificationAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        // Registo estruturado para integração com audit trail e observabilidade
        logger.LogInformation(
            "NotificationAudit: Action={ActionType} Resource={ResourceType}/{ResourceId} " +
            "Tenant={TenantId} User={PerformedBy} Description={Description}",
            entry.ActionType,
            entry.ResourceType,
            entry.ResourceId,
            entry.TenantId,
            entry.PerformedBy,
            entry.Description);

        return Task.CompletedTask;
    }
}
