using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Ponte entre o módulo Identity e o módulo Audit central da plataforma.
///
/// Objetivo: propagar eventos de segurança relevantes do Identity para a trilha de auditoria
/// central, garantindo que eventos como login, role change e password change apareçam no
/// Audit Log unificado consultável por auditores e compliance.
///
/// Design consciente:
/// - Identity.Application define esta interface sem conhecer o Audit module diretamente.
/// - A implementação em Identity.Infrastructure usa IAuditModule (contrato do Audit module).
/// - O acoplamento entre módulos fica restrito à camada de infraestrutura.
/// - Eventos de segurança são armazenados em SecurityEvent (Identity) E no AuditLog (Audit),
///   pois servem propósitos distintos: anomaly detection vs. compliance trail.
///
/// Quando chamar:
/// - Após persistir um SecurityEvent relevante em handlers críticos.
/// - Não bloquear a operação principal em caso de falha — use fire-and-forget com log de erro.
/// </summary>
public interface ISecurityAuditBridge
{
    /// <summary>
    /// Propaga um evento de segurança do módulo Identity para a trilha de auditoria central.
    /// </summary>
    /// <param name="securityEvent">Evento de segurança a ser registrado no Audit central.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task PropagateAsync(SecurityEvent securityEvent, CancellationToken cancellationToken);
}
