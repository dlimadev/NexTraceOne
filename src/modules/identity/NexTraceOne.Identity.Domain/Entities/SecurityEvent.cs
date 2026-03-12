using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Identity.Domain.Entities;

/// <summary>
/// Entidade que registra eventos de segurança e anomalias detectadas no módulo Identity.
///
/// Alimenta o motor de Session Intelligence / Anomaly Detection monitorando:
/// - Volume incomum de aprovações em curto período.
/// - Acesso de localização/IP geográfica nova.
/// - Acesso fora do horário habitual.
/// - Múltiplas sessões simultâneas.
/// - Aprovações em menos de 10 segundos.
/// - Acesso a recursos nunca acessados antes.
///
/// Cada evento possui um nível de risco (score 0-100) e pode desencadear respostas:
/// - Alerta ao administrador.
/// - MFA adicional (step-up authentication).
/// - Suspensão de sessão.
/// - Notificação ao próprio usuário.
///
/// Os eventos são persistidos imutavelmente para trilha de auditoria e análise forense.
/// </summary>
public sealed class SecurityEvent : Entity<SecurityEventId>
{
    private SecurityEvent() { }

    /// <summary>Tenant onde o evento de segurança ocorreu.</summary>
    public TenantId TenantId { get; private set; } = null!;

    /// <summary>Usuário associado ao evento, quando identificável.</summary>
    public UserId? UserId { get; private set; }

    /// <summary>Sessão associada ao evento, quando aplicável.</summary>
    public SessionId? SessionId { get; private set; }

    /// <summary>
    /// Tipo do evento de segurança.
    /// Códigos padronizados definidos em <see cref="SecurityEventType"/>.
    /// </summary>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>Descrição técnica do evento para análise e investigação.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Score de risco de 0 (sem risco) a 100 (risco crítico).
    /// Utilizado para priorização de alertas e decisão de resposta automática.
    /// </summary>
    public int RiskScore { get; private set; }

    /// <summary>IP de origem do evento.</summary>
    public string? IpAddress { get; private set; }

    /// <summary>User agent de origem do evento.</summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// Metadados adicionais em JSON para contexto expandido.
    /// Exemplo: geolocalização, dispositivo, horário habitual, contagem de aprovações recentes.
    /// </summary>
    public string? MetadataJson { get; private set; }

    /// <summary>Data/hora UTC do evento.</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>Indica se o evento já foi revisado por um administrador.</summary>
    public bool IsReviewed { get; private set; }

    /// <summary>Data/hora UTC da revisão, quando aplicável.</summary>
    public DateTimeOffset? ReviewedAt { get; private set; }

    /// <summary>Usuário que revisou o evento.</summary>
    public UserId? ReviewedBy { get; private set; }

    /// <summary>Cria um novo evento de segurança.</summary>
    public static SecurityEvent Create(
        TenantId tenantId,
        UserId? userId,
        SessionId? sessionId,
        string eventType,
        string description,
        int riskScore,
        string? ipAddress,
        string? userAgent,
        string? metadataJson,
        DateTimeOffset now)
    {
        Guard.Against.Null(tenantId);
        Guard.Against.NullOrWhiteSpace(eventType);
        Guard.Against.NullOrWhiteSpace(description);

        return new SecurityEvent
        {
            Id = SecurityEventId.New(),
            TenantId = tenantId,
            UserId = userId,
            SessionId = sessionId,
            EventType = eventType,
            Description = description,
            RiskScore = Math.Clamp(riskScore, 0, 100),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            MetadataJson = metadataJson,
            OccurredAt = now,
            IsReviewed = false
        };
    }

    /// <summary>Marca o evento como revisado por um administrador.</summary>
    public void MarkReviewed(UserId reviewedBy, DateTimeOffset now)
    {
        Guard.Against.Null(reviewedBy);

        IsReviewed = true;
        ReviewedBy = reviewedBy;
        ReviewedAt = now;
    }
}

/// <summary>
/// Tipos padronizados de eventos de segurança para o motor de Session Intelligence.
/// Os códigos são estáveis e servem como chaves para filtros, alertas e i18n.
/// </summary>
public static class SecurityEventType
{
    /// <summary>Falha de autenticação (senha inválida, token expirado).</summary>
    public const string AuthenticationFailed = "security.auth.failed";

    /// <summary>Autenticação local bem-sucedida.</summary>
    public const string AuthenticationSucceeded = "security.auth.succeeded";

    /// <summary>Logout realizado pelo usuário.</summary>
    public const string LogoutPerformed = "security.auth.logout";

    /// <summary>Conta bloqueada por excesso de tentativas.</summary>
    public const string AccountLocked = "security.auth.account_locked";

    /// <summary>Login de IP/localização desconhecida para o usuário.</summary>
    public const string UnknownLocation = "security.anomaly.unknown_location";

    /// <summary>Acesso fora do horário habitual do usuário.</summary>
    public const string OutsideBusinessHours = "security.anomaly.outside_hours";

    /// <summary>Múltiplas sessões simultâneas ativas.</summary>
    public const string ConcurrentSessions = "security.anomaly.concurrent_sessions";

    /// <summary>Aprovação suspeitamente rápida (menos de 10 segundos).</summary>
    public const string RapidApproval = "security.anomaly.rapid_approval";

    /// <summary>Volume incomum de aprovações em curto período.</summary>
    public const string HighApprovalVolume = "security.anomaly.high_approval_volume";

    /// <summary>Acesso a recurso nunca acessado antes pelo usuário.</summary>
    public const string FirstAccessToResource = "security.anomaly.first_resource_access";

    /// <summary>Ativação de acesso emergencial (Break Glass).</summary>
    public const string BreakGlassActivated = "security.privileged.break_glass_activated";

    /// <summary>Solicitação de acesso JIT criada.</summary>
    public const string JitAccessRequested = "security.privileged.jit_requested";

    /// <summary>Delegação formal criada.</summary>
    public const string DelegationCreated = "security.privileged.delegation_created";

    /// <summary>Sessão suspensa por anomalia detectada.</summary>
    public const string SessionSuspended = "security.response.session_suspended";

    /// <summary>MFA adicional solicitado (step-up).</summary>
    public const string StepUpMfaRequired = "security.response.stepup_mfa_required";

    // ── Gestão de Identidade ─────────────────────────────────────────────

    /// <summary>
    /// Papel/perfil atribuído ou alterado para um usuário em um tenant.
    /// Evento crítico para trilha de auditoria de conformidade (SOX, LGPD, ISO 27001).
    /// </summary>
    public const string RoleAssigned = "security.identity.role_assigned";

    /// <summary>
    /// Papel/perfil removido de um usuário em um tenant.
    /// Registrado quando a associação é desativada ou substituída.
    /// </summary>
    public const string RoleRevoked = "security.identity.role_revoked";

    /// <summary>
    /// Senha do usuário alterada com sucesso pelo próprio usuário (self-service).
    /// </summary>
    public const string PasswordChanged = "security.identity.password_changed";

    /// <summary>
    /// Senha do usuário resetada por um administrador.
    /// Requer justificativa e gera alerta ao próprio usuário.
    /// </summary>
    public const string PasswordResetByAdmin = "security.identity.password_reset_admin";

    /// <summary>
    /// Tentativa de alteração de senha falhou por senha atual incorreta.
    /// Múltiplas falhas consecutivas podem indicar ataque de força bruta.
    /// </summary>
    public const string PasswordChangeFailed = "security.identity.password_change_failed";

    // ── Autorização por Ambiente ─────────────────────────────────────────

    /// <summary>
    /// Acesso negado a um ambiente específico (e.g., Production) por falta de permissão.
    /// Indica tentativa de acesso não autorizado a um escopo mais elevado.
    /// </summary>
    public const string EnvironmentAccessDenied = "security.environment.access_denied";

    /// <summary>Permissão de acesso concedida a um ambiente específico para o usuário.</summary>
    public const string EnvironmentAccessGranted = "security.environment.access_granted";

    /// <summary>Permissão de acesso a um ambiente específico revogada do usuário.</summary>
    public const string EnvironmentAccessRevoked = "security.environment.access_revoked";

    // ── Access Review ────────────────────────────────────────────────────

    /// <summary>Campanha de recertificação de acessos iniciada para o tenant.</summary>
    public const string AccessReviewStarted = "security.access_review.started";

    /// <summary>Item de revisão de acesso aprovado (acesso confirmado).</summary>
    public const string AccessReviewItemApproved = "security.access_review.item_approved";

    /// <summary>Item de revisão de acesso revogado (acesso removido).</summary>
    public const string AccessReviewItemRevoked = "security.access_review.item_revoked";

    /// <summary>
    /// Itens de revisão não decididos dentro do prazo foram automaticamente revogados.
    /// Mecanismo de segurança para garantir que acessos não revisados não persistam.
    /// </summary>
    public const string AccessReviewExpiredAutoRevoked = "security.access_review.expired_auto_revoked";

    // ── OIDC / Federação ─────────────────────────────────────────────────

    /// <summary>Fluxo OIDC iniciado — redirecionamento para o provider externo.</summary>
    public const string OidcFlowStarted = "security.oidc.flow_started";

    /// <summary>Callback OIDC recebido e identidade federada vinculada com sucesso.</summary>
    public const string OidcCallbackSuccess = "security.oidc.callback_success";

    /// <summary>Callback OIDC falhou — token inválido, state adulterado ou provider rejeitou.</summary>
    public const string OidcCallbackFailed = "security.oidc.callback_failed";
}

/// <summary>Identificador fortemente tipado de SecurityEvent.</summary>
public sealed record SecurityEventId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static SecurityEventId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static SecurityEventId From(Guid id) => new(id);
}
