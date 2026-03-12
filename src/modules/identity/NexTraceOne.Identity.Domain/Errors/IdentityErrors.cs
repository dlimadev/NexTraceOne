using NexTraceOne.BuildingBlocks.Domain.Results;

namespace NexTraceOne.Identity.Domain.Errors;

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
}
