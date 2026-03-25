namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>
/// Catálogo de constantes que representam os tipos padronizados de eventos de segurança
/// para o motor de Session Intelligence / Anomaly Detection.
///
/// Extraído de <see cref="SecurityEvent"/> para aderência ao princípio de responsabilidade
/// única (SRP): a entidade cuida do ciclo de vida do evento, enquanto esta classe mantém
/// o vocabulário controlado de tipos.
///
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
    /// Novo usuário criado na plataforma dentro de um tenant.
    /// Evento crítico para trilha de auditoria de conformidade (SOX, LGPD, ISO 27001).
    /// </summary>
    public const string UserCreated = "security.identity.user_created";

    /// <summary>Usuário desativado por um administrador, impedindo novos logins.</summary>
    public const string UserDeactivated = "security.identity.user_deactivated";

    /// <summary>Usuário reativado por um administrador.</summary>
    public const string UserActivated = "security.identity.user_activated";

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

    // ── Expirações Automáticas ───────────────────────────────────────────

    /// <summary>Delegação formal expirada automaticamente ao fim da vigência.</summary>
    public const string DelegationExpired = "security.privileged.delegation_expired";

    /// <summary>Acesso emergencial (Break Glass) expirado automaticamente.</summary>
    public const string BreakGlassExpired = "security.privileged.break_glass_expired";

    /// <summary>Acesso JIT expirado (sem aprovação no prazo ou grant encerrado).</summary>
    public const string JitAccessExpired = "security.privileged.jit_expired";

    /// <summary>Acesso a ambiente expirado automaticamente (grant temporário encerrado).</summary>
    public const string EnvironmentAccessExpired = "security.environment.access_expired";
}
