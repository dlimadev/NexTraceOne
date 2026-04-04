using Microsoft.Extensions.Logging;

using NexTraceOne.AuditCompliance.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Application.Abstractions;

namespace NexTraceOne.Notifications.Infrastructure.Governance;

/// <summary>
/// Implementação do serviço de auditoria de notificações.
/// P7.3 — regista eventos auditáveis reais na trilha de auditoria centralizada do NexTraceOne
/// através do IAuditModule (AuditCompliance.Contracts), que persiste os eventos com hash chain SHA-256.
///
/// Estratégia de falha: best-effort — a operação principal não é bloqueada se a auditoria falhar.
/// Falhas são logadas mas não propagadas, seguindo o padrão do SecurityAuditBridge.
///
/// Eventos auditados:
///   - Notificações críticas geradas/entregues/falhadas
///   - Notificações geradas (todas)
///   - Delivery concluído / falhado / retry agendado
///   - Acknowledge de notificações críticas
///   - Snooze de notificações
///   - Escalation disparado
///   - Mudanças de preferências
///   - Suppression aplicada
/// </summary>
internal sealed class NotificationAuditService(
    IAuditModule auditModule,
    ILogger<NotificationAuditService> logger) : INotificationAuditService
{
    /// <inheritdoc/>
    public async Task RecordAsync(
        NotificationAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var performedBy = entry.PerformedBy?.ToString() ?? "system";

            await auditModule.RecordEventAsync(
                sourceModule: "notifications",
                actionType: entry.ActionType,
                resourceId: entry.ResourceId,
                resourceType: entry.ResourceType,
                performedBy: performedBy,
                tenantId: entry.TenantId,
                payload: BuildPayload(entry),
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            // Best-effort: não bloquear a operação principal se a auditoria falhar
            logger.LogWarning(ex,
                "Notification audit record failed for action {ActionType}/{ResourceId}. " +
                "The notification operation was not affected.",
                entry.ActionType, entry.ResourceId);
        }
    }

    /// <summary>
    /// Constrói o payload JSON combinando a descrição e o payload adicional.
    /// Usa serialização JSON adequada para evitar output malformado.
    /// </summary>
    private static string? BuildPayload(NotificationAuditEntry entry)
    {
        if (entry.Description is null && entry.PayloadJson is null)
            return null;

        var envelope = new Dictionary<string, object?>();

        if (entry.Description is not null)
            envelope["description"] = entry.Description;

        if (entry.PayloadJson is not null)
        {
            try
            {
                var contextElement = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(entry.PayloadJson);
                envelope["context"] = contextElement;
            }
            catch
            {
                // PayloadJson malformado — incluir como string em vez de abortar a auditoria
                System.Diagnostics.Trace.TraceWarning("NotificationAuditService: Failed to deserialize PayloadJson — falling back to raw string.");
                envelope["context"] = entry.PayloadJson;
            }
        }

        return System.Text.Json.JsonSerializer.Serialize(envelope);
    }
}
