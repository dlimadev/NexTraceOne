using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;

namespace NexTraceOne.Identity.Application.Features;

/// <summary>
/// Serviço utilitário interno para registro centralizado de eventos de segurança (SecurityEvent)
/// no módulo Identity.
///
/// Extraído dos handlers LocalLogin e OidcCallback para eliminar a duplicação de lógica
/// de criação de SecurityEvent espalhada entre múltiplos handlers de autenticação.
///
/// Responsabilidade única: encapsular a criação e persistência de SecurityEvents,
/// isolando os handlers da mecânica de construção de eventos de auditoria.
///
/// Decisão de design:
/// - Classe interna (internal) — usada apenas dentro do módulo Identity.
/// - Sem estado — cada método é autocontido, recebendo todos os parâmetros necessários.
/// - Sem lógica de negócio — apenas formata e persiste o evento.
/// - Handlers continuam decidindo QUANDO registrar — este serviço apenas executa o COMO.
/// </summary>
internal static class SecurityAuditRecorder
{
    /// <summary>
    /// Registra evento de autenticação bem-sucedida para trilha de auditoria.
    /// Usado por LocalLogin e OidcCallback após validação completa de credenciais.
    /// </summary>
    public static void RecordAuthenticationSuccess(
        ISecurityEventRepository securityEventRepository,
        IDateTimeProvider dateTimeProvider,
        TenantId tenantId,
        UserId userId,
        string? ipAddress,
        string? userAgent,
        string? metadataJson = null)
    {
        securityEventRepository.Add(SecurityEvent.Create(
            tenantId,
            userId,
            sessionId: null,
            SecurityEventType.AuthenticationSucceeded,
            $"Authentication succeeded for user '{userId.Value}'.",
            riskScore: 0,
            ipAddress,
            userAgent,
            metadataJson,
            dateTimeProvider.UtcNow));
    }

    /// <summary>
    /// Registra evento de falha de autenticação para detecção de anomalias.
    /// Usado por LocalLogin quando credenciais são inválidas ou usuário não encontrado.
    /// </summary>
    public static void RecordAuthenticationFailure(
        ISecurityEventRepository securityEventRepository,
        IDateTimeProvider dateTimeProvider,
        TenantId tenantId,
        UserId? userId,
        string reason,
        string? ipAddress,
        string? userAgent)
    {
        securityEventRepository.Add(SecurityEvent.Create(
            tenantId,
            userId,
            sessionId: null,
            SecurityEventType.AuthenticationFailed,
            $"Authentication failed: {reason}",
            riskScore: 30,
            ipAddress,
            userAgent,
            metadataJson: null,
            dateTimeProvider.UtcNow));
    }

    /// <summary>
    /// Registra evento de bloqueio de conta por tentativas excessivas de login.
    /// Risk score elevado (70) pois pode indicar ataque de força bruta.
    /// </summary>
    public static void RecordAccountLocked(
        ISecurityEventRepository securityEventRepository,
        IDateTimeProvider dateTimeProvider,
        TenantId tenantId,
        UserId userId,
        string? ipAddress,
        string? userAgent)
    {
        securityEventRepository.Add(SecurityEvent.Create(
            tenantId,
            userId,
            sessionId: null,
            SecurityEventType.AccountLocked,
            $"Account locked for user '{userId.Value}' due to excessive failed login attempts.",
            riskScore: 70,
            ipAddress,
            userAgent,
            metadataJson: null,
            dateTimeProvider.UtcNow));
    }

    /// <summary>
    /// Registra evento de callback OIDC bem-sucedido para trilha de auditoria federada.
    /// Inclui metadados do provider e identidade externa no evento.
    /// </summary>
    public static void RecordOidcCallbackSuccess(
        ISecurityEventRepository securityEventRepository,
        IDateTimeProvider dateTimeProvider,
        TenantId tenantId,
        UserId userId,
        SessionId sessionId,
        string provider,
        string externalId,
        string? ipAddress,
        string? userAgent)
    {
        securityEventRepository.Add(SecurityEvent.Create(
            tenantId,
            userId,
            sessionId,
            SecurityEventType.OidcCallbackSuccess,
            $"OIDC callback processed successfully for provider '{provider}', user '{userId.Value}'.",
            riskScore: 0,
            ipAddress,
            userAgent,
            $"{{\"provider\":\"{provider}\",\"externalId\":\"{externalId}\"}}",
            dateTimeProvider.UtcNow));
    }

    /// <summary>
    /// Registra evento de falha no callback OIDC para detecção de anomalias federadas.
    /// Risk score moderado (50) pois pode indicar replay attack ou provider comprometido.
    /// </summary>
    public static void RecordOidcCallbackFailure(
        ISecurityEventRepository securityEventRepository,
        IDateTimeProvider dateTimeProvider,
        TenantId tenantId,
        string provider,
        string reason,
        string? ipAddress,
        string? userAgent)
    {
        securityEventRepository.Add(SecurityEvent.Create(
            tenantId,
            userId: null,
            sessionId: null,
            SecurityEventType.OidcCallbackFailed,
            $"OIDC callback failed for provider '{provider}': {reason}",
            riskScore: 50,
            ipAddress,
            userAgent,
            $"{{\"provider\":\"{provider}\"}}",
            dateTimeProvider.UtcNow));
    }

    /// <summary>
    /// Resolve o TenantId para eventos de segurança quando o tenant pode não estar disponível.
    /// Usado em cenários de falha de autenticação onde o contexto do tenant pode estar vazio.
    /// Retorna TenantId.From(Guid.Empty) como fallback seguro.
    /// </summary>
    public static TenantId ResolveTenantIdForAudit(ICurrentTenant currentTenant)
    {
        return currentTenant.Id != Guid.Empty
            ? TenantId.From(currentTenant.Id)
            : TenantId.From(Guid.Empty);
    }
}
