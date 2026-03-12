using Microsoft.Extensions.Logging;
using NexTraceOne.Audit.Contracts.ServiceInterfaces;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;

namespace NexTraceOne.Identity.Infrastructure.Services;

/// <summary>
/// Implementação da ponte entre o módulo Identity e o módulo Audit central.
///
/// Traduz SecurityEvents do domínio Identity em chamadas ao IAuditModule,
/// que persiste o evento na trilha de auditoria unificada da plataforma.
///
/// Mapeamento:
/// - sourceModule → "identity"
/// - actionType   → SecurityEvent.EventType (e.g., "security.identity.role_assigned")
/// - resourceId   → UserId (recurso afetado)
/// - resourceType → "user"
/// - performedBy  → userId ou "system" para eventos automáticos
/// - tenantId     → TenantId do evento
/// - payload      → MetadataJson para enriquecer a trilha
///
/// Estratégia de falha: best-effort (não bloqueia a operação principal).
/// Falhas são logadas mas não propagadas para não impactar o fluxo de negócio.
/// Em produção, considerar Outbox para garantia de entrega.
/// </summary>
internal sealed class SecurityAuditBridge(
    IAuditModule auditModule,
    ILogger<SecurityAuditBridge> logger) : ISecurityAuditBridge
{
    /// <inheritdoc />
    public async Task PropagateAsync(SecurityEvent securityEvent, CancellationToken cancellationToken)
    {
        try
        {
            var performedBy = securityEvent.UserId?.Value.ToString() ?? "system";
            var resourceId = securityEvent.UserId?.Value.ToString() ?? "unknown";

            await auditModule.RecordEventAsync(
                sourceModule: "identity",
                actionType: securityEvent.EventType,
                resourceId: resourceId,
                resourceType: "user",
                performedBy: performedBy,
                tenantId: securityEvent.TenantId.Value,
                payload: securityEvent.MetadataJson,
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Falha na propagação não deve bloquear a operação principal.
            // O SecurityEvent já foi persistido no módulo Identity — a perda
            // é apenas na cópia do Audit central, que pode ser reconciliada.
            logger.LogError(
                ex,
                "Failed to propagate SecurityEvent '{EventType}' for user '{UserId}' to central audit. " +
                "The security event was persisted in Identity module but not in Audit module.",
                securityEvent.EventType,
                securityEvent.UserId?.Value);
        }
    }
}
