using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Contrato para registro centralizado de eventos de segurança (SecurityEvent)
/// no módulo Identity.
///
/// Responsabilidade única: encapsular a criação e persistência de SecurityEvents,
/// isolando os handlers da mecânica de construção de eventos de auditoria.
///
/// Decisão de design:
/// - Interface injetável via DI — permite mock em testes e respeita DIP.
/// - Implementação Scoped — compartilha repositórios do request corrente.
/// - Handlers decidem QUANDO registrar — este serviço executa o COMO.
///
/// Refatoração: extraído da classe estática SecurityAuditRecorder para aderir
/// ao Dependency Inversion Principle e facilitar testes unitários.
/// </summary>
public interface ISecurityAuditRecorder
{
    /// <summary>
    /// Registra evento de autenticação bem-sucedida para trilha de auditoria.
    /// Usado por LocalLogin e OidcCallback após validação completa de credenciais.
    /// </summary>
    void RecordAuthenticationSuccess(
        TenantId tenantId,
        UserId userId,
        string? ipAddress,
        string? userAgent,
        string? metadataJson = null);

    /// <summary>
    /// Registra evento de falha de autenticação para detecção de anomalias.
    /// Usado por LocalLogin quando credenciais são inválidas ou usuário não encontrado.
    /// </summary>
    void RecordAuthenticationFailure(
        TenantId tenantId,
        UserId? userId,
        string reason,
        string? ipAddress,
        string? userAgent);

    /// <summary>
    /// Registra evento de bloqueio de conta por tentativas excessivas de login.
    /// Risk score elevado (70) pois pode indicar ataque de força bruta.
    /// </summary>
    void RecordAccountLocked(
        TenantId tenantId,
        UserId userId,
        string? ipAddress,
        string? userAgent);

    /// <summary>
    /// Registra evento de callback OIDC bem-sucedido para trilha de auditoria federada.
    /// Inclui metadados do provider e identidade externa no evento.
    /// </summary>
    void RecordOidcCallbackSuccess(
        TenantId tenantId,
        UserId userId,
        SessionId sessionId,
        string provider,
        string externalId,
        string? ipAddress,
        string? userAgent);

    /// <summary>
    /// Registra evento de falha no callback OIDC para detecção de anomalias federadas.
    /// Risk score moderado (50) pois pode indicar replay attack ou provider comprometido.
    /// </summary>
    void RecordOidcCallbackFailure(
        TenantId tenantId,
        string provider,
        string reason,
        string? ipAddress,
        string? userAgent);

    /// <summary>
    /// Resolve o TenantId para eventos de segurança quando o tenant pode não estar disponível.
    /// Usado em cenários de falha de autenticação onde o contexto do tenant pode estar vazio.
    /// Retorna TenantId.From(Guid.Empty) como fallback seguro.
    /// </summary>
    TenantId ResolveTenantIdForAudit();
}
