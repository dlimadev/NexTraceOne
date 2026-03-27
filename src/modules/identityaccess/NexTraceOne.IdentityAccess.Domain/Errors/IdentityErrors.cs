using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.IdentityAccess.Domain.Errors;

/// <summary>Catálogo centralizado de erros do módulo Identity com códigos i18n.</summary>
public static class IdentityErrors
{
    /// <summary>Usuário não encontrado.</summary>
    public static Error UserNotFound(Guid userId)
        => Error.NotFound("Identity.User.NotFound", "User '{0}' was not found.", userId);

    /// <summary>Email já cadastrado.</summary>
    public static Error EmailAlreadyExists(string email)
        => Error.Conflict("Identity.User.EmailAlreadyExists", "Email '{0}' is already registered.", email);

    /// <summary>Credenciais inválidas.</summary>
    public static Error InvalidCredentials()
        => Error.Unauthorized("Identity.Auth.InvalidCredentials", "The provided credentials are invalid.");

    /// <summary>Conta bloqueada por tentativas inválidas.</summary>
    public static Error AccountLocked(DateTimeOffset? lockoutEnd)
        => Error.Forbidden("Identity.Auth.AccountLocked", "The account is locked until '{0:O}'.", lockoutEnd ?? DateTimeOffset.MinValue);

    /// <summary>Conta desativada.</summary>
    public static Error AccountDeactivated(Guid userId)
        => Error.Forbidden("Identity.Auth.AccountDeactivated", "The account '{0}' has been deactivated.", userId);

    /// <summary>Sessão expirada.</summary>
    public static Error SessionExpired(Guid sessionId)
        => Error.Unauthorized("Identity.Session.Expired", "Session '{0}' has expired.", sessionId);

    /// <summary>Sessão revogada.</summary>
    public static Error SessionRevoked(Guid sessionId)
        => Error.Unauthorized("Identity.Session.Revoked", "Session '{0}' has been revoked.", sessionId);

    /// <summary>Refresh token inválido.</summary>
    public static Error InvalidRefreshToken()
        => Error.Unauthorized("Identity.Session.InvalidRefreshToken", "The supplied refresh token is invalid.");

    /// <summary>Papel não encontrado.</summary>
    public static Error RoleNotFound(Guid roleId)
        => Error.NotFound("Identity.Role.NotFound", "Role '{0}' was not found.", roleId);

    /// <summary>Vínculo de tenant não encontrado.</summary>
    public static Error TenantMembershipNotFound(Guid userId, Guid tenantId)
        => Error.NotFound("Identity.TenantMembership.NotFound", "Membership for user '{0}' in tenant '{1}' was not found.", userId, tenantId);

    /// <summary>Tenant não encontrado ou inativo.</summary>
    public static Error TenantNotFound(Guid tenantId)
        => Error.NotFound("Identity.Tenant.NotFound", "Tenant '{0}' was not found or is inactive.", tenantId);

    /// <summary>Slug de tenant já existente.</summary>
    public static Error TenantSlugAlreadyExists(string slug)
        => Error.Conflict("Identity.Tenant.SlugAlreadyExists", "A tenant with slug '{0}' already exists.", slug);

    /// <summary>Vínculo de tenant já existente.</summary>
    public static Error MembershipAlreadyExists(Guid userId, Guid tenantId)
        => Error.Conflict("Identity.TenantMembership.AlreadyExists", "Membership for user '{0}' in tenant '{1}' already exists.", userId, tenantId);

    /// <summary>Sessão não encontrada.</summary>
    public static Error SessionNotFound(Guid sessionId)
        => Error.NotFound("Identity.Session.NotFound", "Session '{0}' was not found.", sessionId);

    /// <summary>Senha atual inválida na troca de senha.</summary>
    public static Error CurrentPasswordInvalid()
        => Error.Validation("Identity.User.CurrentPasswordInvalid", "The current password is incorrect.");

    /// <summary>Usuário não autenticado (sem token ou token inválido).</summary>
    public static Error NotAuthenticated()
        => Error.Unauthorized("Identity.Auth.NotAuthenticated", "Authentication is required to access this resource.");

    /// <summary>Usuário não possui a permissão necessária.</summary>
    public static Error InsufficientPermissions(string permission)
        => Error.Forbidden("Identity.Auth.InsufficientPermissions", "You do not have the required permission: '{0}'.", permission);

    // ── Break Glass ──────────────────────────────────────────────────────

    /// <summary>Solicitação Break Glass não encontrada.</summary>
    public static Error BreakGlassNotFound(Guid requestId)
        => Error.NotFound("Identity.BreakGlass.NotFound", "Break glass request '{0}' was not found.", requestId);

    /// <summary>Limite trimestral de Break Glass atingido — requer revisão de segurança.</summary>
    public static Error BreakGlassQuotaExceeded(Guid userId, int usageCount)
        => Error.Forbidden("Identity.BreakGlass.QuotaExceeded", "User '{0}' has reached the quarterly break glass limit ({1} uses). Security review required.", userId, usageCount);

    /// <summary>Break Glass já não está ativo e não pode ser revogado.</summary>
    public static Error BreakGlassNotActive(Guid requestId)
        => Error.Validation("Identity.BreakGlass.NotActive", "Break glass request '{0}' is not currently active.", requestId);

    // ── JIT Access ───────────────────────────────────────────────────────

    /// <summary>Solicitação JIT não encontrada.</summary>
    public static Error JitAccessNotFound(Guid requestId)
        => Error.NotFound("Identity.JitAccess.NotFound", "JIT access request '{0}' was not found.", requestId);

    /// <summary>Auto-aprovação não permitida em solicitações JIT.</summary>
    public static Error JitSelfApprovalNotAllowed()
        => Error.Forbidden("Identity.JitAccess.SelfApprovalNotAllowed", "Self-approval is not allowed for JIT access requests.");

    /// <summary>Solicitação JIT não está pendente (já decidida ou expirada).</summary>
    public static Error JitAccessNotPending(Guid requestId)
        => Error.Validation("Identity.JitAccess.NotPending", "JIT access request '{0}' is not in a pending state.", requestId);

    // ── Delegation ───────────────────────────────────────────────────────

    /// <summary>Delegação não encontrada.</summary>
    public static Error DelegationNotFound(Guid delegationId)
        => Error.NotFound("Identity.Delegation.NotFound", "Delegation '{0}' was not found.", delegationId);

    /// <summary>Tentativa de delegar permissões que o delegante não possui.</summary>
    public static Error DelegationScopeExceedsGrantor()
        => Error.Forbidden("Identity.Delegation.ScopeExceedsGrantor", "The requested delegation scope exceeds the grantor's permissions.");

    /// <summary>Tentativa de delegar permissões de administração de sistema.</summary>
    public static Error DelegationOfSystemAdminNotAllowed()
        => Error.Forbidden("Identity.Delegation.SystemAdminNotAllowed", "Delegation of system administration permissions is not allowed.");

    /// <summary>Auto-delegação não permitida.</summary>
    public static Error DelegationToSelfNotAllowed()
        => Error.Validation("Identity.Delegation.SelfNotAllowed", "A user cannot delegate permissions to themselves.");

    // ── Access Review ────────────────────────────────────────────────────

    /// <summary>Campanha de access review não encontrada.</summary>
    public static Error AccessReviewCampaignNotFound(Guid campaignId)
        => Error.NotFound("Identity.AccessReview.CampaignNotFound", "Access review campaign '{0}' was not found.", campaignId);

    /// <summary>Item de access review não encontrado.</summary>
    public static Error AccessReviewItemNotFound(Guid itemId)
        => Error.NotFound("Identity.AccessReview.ItemNotFound", "Access review item '{0}' was not found.", itemId);

    // ── SSO / External Identity ──────────────────────────────────────────

    /// <summary>
    /// Contexto de tenant obrigatório para a operação.
    /// O caller deve estar autenticado com um tenant válido no JWT ou header.
    /// </summary>
    public static Error TenantContextRequired()
        => Error.Forbidden("Identity.Tenant.ContextRequired", "A valid tenant context is required for this operation.");

    /// <summary>Operação proibida — o caller não tem permissão para executar esta ação.</summary>
    public static Error Forbidden()
        => Error.Forbidden("Identity.Auth.Forbidden", "You are not authorized to perform this operation.");

    /// <summary>Item de access review já foi decidido anteriormente — re-decisão não é permitida.</summary>
    public static Error AccessReviewItemAlreadyDecided(Guid itemId)
        => Error.Validation("Identity.AccessReview.ItemAlreadyDecided", "Access review item '{0}' has already been decided and cannot be changed.", itemId);

    /// <summary>Identidade externa não encontrada.</summary>
    public static Error ExternalIdentityNotFound(string provider, string externalId)
        => Error.NotFound("Identity.ExternalIdentity.NotFound", "External identity for provider '{0}' with external ID '{1}' was not found.", provider, externalId);

    /// <summary>Mapeamento de grupo SSO não encontrado.</summary>
    public static Error SsoGroupMappingNotFound(Guid mappingId)
        => Error.NotFound("Identity.SsoGroupMapping.NotFound", "SSO group mapping '{0}' was not found.", mappingId);

    // ── OIDC / Federação ─────────────────────────────────────────────────

    /// <summary>
    /// Provider OIDC não configurado para este tenant.
    /// O admin deve configurar o provider antes de usar o fluxo federado.
    /// </summary>
    public static Error OidcProviderNotConfigured(string provider)
        => Error.Validation("Identity.Oidc.ProviderNotConfigured", "OIDC provider '{0}' is not configured for this tenant.", provider);

    /// <summary>
    /// Callback OIDC falhou — code inválido, expirado ou provider indisponível.
    /// O usuário deve tentar novamente o fluxo de autenticação.
    /// </summary>
    public static Error OidcCallbackFailed(string provider)
        => Error.Unauthorized("Identity.Oidc.CallbackFailed", "OIDC authentication callback failed for provider '{0}'. Please try again.", provider);

    // ── Environment ──────────────────────────────────────────────────────

    /// <summary>Ambiente não encontrado.</summary>
    public static Error EnvironmentNotFound(Guid environmentId)
        => Error.NotFound("Identity.Environment.NotFound", "Environment '{0}' was not found.", environmentId);

    /// <summary>Slug de ambiente já existente no tenant.</summary>
    public static Error EnvironmentSlugAlreadyExists(string slug)
        => Error.Conflict("Identity.Environment.SlugAlreadyExists", "An environment with slug '{0}' already exists in this tenant.", slug);

    /// <summary>Acesso ao ambiente negado.</summary>
    public static Error EnvironmentAccessDenied(Guid userId, Guid environmentId)
        => Error.Forbidden("Identity.Environment.AccessDenied", "User '{0}' does not have access to environment '{1}'.", userId, environmentId);

    /// <summary>Acesso ao ambiente já existente.</summary>
    public static Error EnvironmentAccessAlreadyExists(Guid userId, Guid environmentId)
        => Error.Conflict("Identity.Environment.AccessAlreadyExists", "User '{0}' already has access to environment '{1}'.", userId, environmentId);

    /// <summary>Ambiente não está ativo — operação não permitida.</summary>
    public static Error EnvironmentNotActive(Guid environmentId)
        => Error.Validation("Identity.Environment.NotActive", "Environment '{0}' is not active.", environmentId);

    /// <summary>Nível de acesso ao ambiente inválido.</summary>
    public static Error InvalidEnvironmentAccessLevel(string accessLevel)
        => Error.Validation("Identity.Environment.InvalidAccessLevel", "Access level '{0}' is not valid. Valid levels: read, write, admin, none.", accessLevel);

    /// <summary>Já existe um ambiente produtivo principal ativo neste tenant.</summary>
    public static Error PrimaryProductionAlreadyExists(Guid tenantId)
        => Error.Conflict("Identity.Environment.PrimaryProductionAlreadyExists", "Tenant '{0}' already has an active primary production environment. Revoke the current designation before setting a new one.", tenantId);

    /// <summary>Ambiente não pertence ao tenant especificado.</summary>
    public static Error EnvironmentNotBelongsToTenant(Guid environmentId, Guid tenantId)
        => Error.Forbidden("Identity.Environment.NotBelongsToTenant", "Environment '{0}' does not belong to tenant '{1}'.", environmentId, tenantId);

    /// <summary>Ambiente inativo não pode ser designado como produção principal.</summary>
    public static Error CannotDesignateInactiveAsPrimaryProduction(Guid environmentId)
        => Error.Validation("Identity.Environment.CannotDesignateInactiveAsPrimaryProduction", "Environment '{0}' is not active and cannot be designated as the primary production environment.", environmentId);

    // ── MFA ──────────────────────────────────────────────────────────────

    /// <summary>Token de desafio MFA inválido ou expirado.</summary>
    public static Error MfaChallengeExpiredOrInvalid()
        => Error.Unauthorized("Identity.Mfa.ChallengeExpiredOrInvalid", "The MFA challenge token is invalid or has expired. Please start the login flow again.");

    /// <summary>Código MFA inválido.</summary>
    public static Error MfaCodeInvalid()
        => Error.Forbidden("Identity.Mfa.CodeInvalid", "The provided MFA code is invalid. Please check your authenticator and try again.");

    /// <summary>Step-up MFA necessário para operação privilegiada.</summary>
    public static Error MfaStepUpRequired()
        => Error.Forbidden("Identity.Mfa.StepUpRequired", "This operation requires MFA step-up verification. Please provide your MFA code.");
}
