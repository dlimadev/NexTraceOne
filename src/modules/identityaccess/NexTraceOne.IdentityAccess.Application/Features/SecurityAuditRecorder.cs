using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Features;

/// <summary>
/// Implementação injetável do registro centralizado de eventos de segurança (SecurityEvent)
/// no módulo Identity.
///
/// Responsabilidade única: encapsular a criação e persistência de SecurityEvents,
/// isolando os handlers da mecânica de construção de eventos de auditoria.
///
/// Decisão de design:
/// - Classe injetável via DI (Scoped) — compartilha repositórios e contexto do request.
/// - Dependências recebidas por construtor — respeita DIP, permite mock em testes.
/// - Sem lógica de negócio — apenas formata e persiste o evento.
/// - Handlers decidem QUANDO registrar — este serviço executa o COMO.
///
/// Refatoração: migrado de classe estática para serviço injetável para aderir
/// ao Dependency Inversion Principle e facilitar testes unitários dos handlers.
/// </summary>
internal sealed class SecurityAuditRecorder(
    ISecurityEventRepository securityEventRepository,
    ISecurityEventTracker securityEventTracker,
    IDateTimeProvider dateTimeProvider,
    ICurrentTenant currentTenant) : ISecurityAuditRecorder
{
    /// <inheritdoc />
    public void RecordAuthenticationSuccess(
        TenantId tenantId,
        UserId userId,
        string? ipAddress,
        string? userAgent,
        string? metadataJson = null)
    {
        var securityEvent = SecurityEvent.Create(
            tenantId,
            userId,
            sessionId: null,
            SecurityEventType.AuthenticationSucceeded,
            $"Authentication succeeded for user '{userId.Value}'.",
            riskScore: 0,
            ipAddress,
            userAgent,
            metadataJson,
            dateTimeProvider.UtcNow);
        securityEventRepository.Add(securityEvent);
        securityEventTracker.Track(securityEvent);
    }

    /// <inheritdoc />
    public void RecordAuthenticationFailure(
        TenantId tenantId,
        UserId? userId,
        string reason,
        string? ipAddress,
        string? userAgent)
    {
        var securityEvent = SecurityEvent.Create(
            tenantId,
            userId,
            sessionId: null,
            SecurityEventType.AuthenticationFailed,
            $"Authentication failed: {reason}",
            riskScore: 30,
            ipAddress,
            userAgent,
            metadataJson: null,
            dateTimeProvider.UtcNow);
        securityEventRepository.Add(securityEvent);
        securityEventTracker.Track(securityEvent);
    }

    /// <inheritdoc />
    public void RecordAccountLocked(
        TenantId tenantId,
        UserId userId,
        string? ipAddress,
        string? userAgent)
    {
        var securityEvent = SecurityEvent.Create(
            tenantId,
            userId,
            sessionId: null,
            SecurityEventType.AccountLocked,
            $"Account locked for user '{userId.Value}' due to excessive failed login attempts.",
            riskScore: 70,
            ipAddress,
            userAgent,
            metadataJson: null,
            dateTimeProvider.UtcNow);
        securityEventRepository.Add(securityEvent);
        securityEventTracker.Track(securityEvent);
    }

    /// <inheritdoc />
    public void RecordOidcCallbackSuccess(
        TenantId tenantId,
        UserId userId,
        SessionId sessionId,
        string provider,
        string externalId,
        string? ipAddress,
        string? userAgent)
    {
        var securityEvent = SecurityEvent.Create(
            tenantId,
            userId,
            sessionId,
            SecurityEventType.OidcCallbackSuccess,
            $"OIDC callback processed successfully for provider '{provider}', user '{userId.Value}'.",
            riskScore: 0,
            ipAddress,
            userAgent,
            $"{{\"provider\":\"{provider}\",\"externalId\":\"{externalId}\"}}",
            dateTimeProvider.UtcNow);
        securityEventRepository.Add(securityEvent);
        securityEventTracker.Track(securityEvent);
    }

    /// <inheritdoc />
    public void RecordOidcCallbackFailure(
        TenantId tenantId,
        string provider,
        string reason,
        string? ipAddress,
        string? userAgent)
    {
        var securityEvent = SecurityEvent.Create(
            tenantId,
            userId: null,
            sessionId: null,
            SecurityEventType.OidcCallbackFailed,
            $"OIDC callback failed for provider '{provider}': {reason}",
            riskScore: 50,
            ipAddress,
            userAgent,
            $"{{\"provider\":\"{provider}\"}}",
            dateTimeProvider.UtcNow);
        securityEventRepository.Add(securityEvent);
        securityEventTracker.Track(securityEvent);
    }

    /// <inheritdoc />
    public TenantId ResolveTenantIdForAudit()
    {
        return currentTenant.Id != Guid.Empty
            ? TenantId.From(currentTenant.Id)
            : TenantId.From(Guid.Empty);
    }
}
