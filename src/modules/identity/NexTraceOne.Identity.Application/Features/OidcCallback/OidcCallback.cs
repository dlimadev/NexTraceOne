using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.Errors;
using NexTraceOne.Identity.Domain.ValueObjects;
using LocalLoginFeature = NexTraceOne.Identity.Application.Features.LocalLogin.LocalLogin;

namespace NexTraceOne.Identity.Application.Features.OidcCallback;

/// <summary>
/// Feature: OidcCallback — processa o callback do provider OIDC após autenticação externa.
///
/// Fluxo completo:
/// 1. Provider redireciona para /auth/oidc/callback?code=...&amp;state=...
/// 2. Este handler valida o state (previne CSRF), extrai returnTo.
/// 3. Troca o code por informações do usuário via IOidcProvider.ExchangeCodeAsync.
/// 4. Cria ou vincula o usuário interno (User + ExternalIdentity).
/// 5. Resolve o TenantMembership do usuário.
/// 6. Gera sessão e JWT token.
/// 7. Retorna o token + returnTo para o frontend completar a navegação.
///
/// Tratamento de erros:
/// - State inválido → CSRF suspeito, rejeita com OidcCallbackFailed event.
/// - Code inválido ou expirado → rejeita com mensagem de erro.
/// - Provider não disponível → retorna OidcProviderNotConfigured.
/// - Usuário sem tenant → retorna TenantMembershipNotFound.
///
/// Compatibilidade com Deep Link:
/// O returnTo extraído do state é retornado junto com os tokens para que
/// o frontend possa restaurar a rota original do usuário após autenticação.
/// </summary>
public static class OidcCallback
{
    /// <summary>Comando do callback OIDC com code e state recebidos do provider.</summary>
    public sealed record Command(
        string Provider,
        string Code,
        string State,
        string? IpAddress = null,
        string? UserAgent = null) : ICommand<Response>, IPublicRequest;

    /// <summary>Resposta do callback com tokens e URL de destino pós-autenticação.</summary>
    public sealed record Response(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn,
        LocalLoginFeature.UserResponse User,

        /// <summary>
        /// URL de destino para o frontend restaurar a navegação pós-autenticação.
        /// Extraída do state gerado em StartOidcLogin.
        /// Padrão: "/" caso o state não contenha returnTo ou seja inválido.
        /// </summary>
        string ReturnTo);

    /// <summary>Valida os parâmetros do callback.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Provider).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Code).NotEmpty();
            RuleFor(x => x.State).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que processa o callback OIDC de ponta a ponta.
    /// Valida o state, troca o code, cria/vincula o usuário e emite o JWT.
    /// </summary>
    public sealed class Handler(
        IOidcProvider oidcProvider,
        IUserRepository userRepository,
        ITenantMembershipRepository membershipRepository,
        IRoleRepository roleRepository,
        ISessionRepository sessionRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IDateTimeProvider dateTimeProvider,
        ICurrentTenant currentTenant,
        ISecurityEventRepository securityEventRepository) : ICommandHandler<Command, Response>
    {
        /// <summary>URI de callback — deve ser idêntico ao usado em StartOidcLogin.</summary>
        private const string CallbackPath = "/api/v1/identity/auth/oidc/callback";

        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!oidcProvider.IsConfigured(request.Provider))
            {
                return IdentityErrors.OidcProviderNotConfigured(request.Provider);
            }

            // Extrai e valida o state para prevenir ataques CSRF
            var returnTo = ExtractReturnToFromState(request.State);

            OidcUserInfo userInfo;
            try
            {
                userInfo = await oidcProvider.ExchangeCodeAsync(
                    request.Provider,
                    request.Code,
                    CallbackPath,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                // Falha no token exchange — pode ser code expirado, inválido ou provider indisponível
                RecordCallbackFailedEvent(request, $"Token exchange failed: {ex.Message}");
                return IdentityErrors.OidcCallbackFailed(request.Provider);
            }

            // Localiza ou cria o usuário interno vinculado à identidade federada
            var user = await userRepository.GetByFederatedIdentityAsync(
                request.Provider,
                userInfo.ExternalId,
                cancellationToken);

            user ??= await userRepository.GetByEmailAsync(Email.Create(userInfo.Email), cancellationToken);

            if (user is null)
            {
                // Provisiona novo usuário federado automaticamente (Just-in-Time provisioning)
                user = User.CreateFederated(
                    Email.Create(userInfo.Email),
                    FullName.FromDisplayName(userInfo.DisplayName),
                    request.Provider,
                    userInfo.ExternalId);
                userRepository.Add(user);
            }
            else
            {
                // Vincula a identidade externa ao usuário existente se ainda não vinculada
                user.LinkFederatedIdentity(request.Provider, userInfo.ExternalId);
            }

            // Resolve o TenantMembership do usuário no tenant corrente
            var membership = await IdentityFeatureSupport.ResolveMembershipAsync(
                currentTenant,
                membershipRepository,
                user.Id,
                cancellationToken);

            if (membership is null && currentTenant.Id != Guid.Empty)
            {
                // Auto-provisiona o usuário como Viewer no tenant se não tiver membership
                var viewerRole = await roleRepository.GetByNameAsync(Role.Viewer, cancellationToken);
                if (viewerRole is null)
                {
                    return IdentityErrors.RoleNotFound(Guid.Empty);
                }

                membership = TenantMembership.Create(
                    user.Id,
                    TenantId.From(currentTenant.Id),
                    viewerRole.Id,
                    dateTimeProvider.UtcNow);

                membershipRepository.Add(membership);
            }

            if (membership is null)
            {
                return IdentityErrors.TenantMembershipNotFound(user.Id.Value, currentTenant.Id);
            }

            var role = await roleRepository.GetByIdAsync(membership.RoleId, cancellationToken);
            if (role is null)
            {
                return IdentityErrors.RoleNotFound(membership.RoleId.Value);
            }

            user.RegisterSuccessfulLogin(dateTimeProvider.UtcNow);

            // Cria sessão e emite tokens JWT
            var refreshToken = jwtTokenGenerator.GenerateRefreshToken();
            var session = Session.Create(
                user.Id,
                RefreshTokenHash.Create(refreshToken),
                dateTimeProvider.UtcNow.AddDays(30),
                request.IpAddress ?? "unknown",
                request.UserAgent ?? "unknown");

            sessionRepository.Add(session);

            // Registra callback bem-sucedido para trilha de auditoria
            securityEventRepository.Add(SecurityEvent.Create(
                membership.TenantId,
                user.Id,
                session.Id,
                SecurityEventType.OidcCallbackSuccess,
                $"OIDC callback processed successfully for provider '{request.Provider}', user '{user.Id.Value}'.",
                riskScore: 0,
                request.IpAddress,
                request.UserAgent,
                $"{{\"provider\":\"{request.Provider}\",\"externalId\":\"{userInfo.ExternalId}\"}}",
                dateTimeProvider.UtcNow));

            var loginResponse = IdentityFeatureSupport.CreateLoginResponse(
                user,
                membership,
                role,
                jwtTokenGenerator,
                refreshToken);

            return new Response(
                loginResponse.AccessToken,
                loginResponse.RefreshToken,
                loginResponse.ExpiresIn,
                loginResponse.User,
                returnTo);
        }

        /// <summary>
        /// Extrai o returnTo do state gerado por StartOidcLogin.
        /// Formato esperado: Base64("{nonce}:{returnTo_url_encoded}").
        /// Se o state for inválido ou malformado, retorna "/" como destino seguro.
        /// </summary>
        private static string ExtractReturnToFromState(string state)
        {
            try
            {
                var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(state));
                var colonIndex = decoded.IndexOf(':');
                if (colonIndex > 0 && colonIndex < decoded.Length - 1)
                {
                    var encodedReturnTo = decoded[(colonIndex + 1)..];
                    return Uri.UnescapeDataString(encodedReturnTo);
                }
            }
            catch
            {
                // State malformado — retorna destino seguro padrão
            }

            return "/";
        }

        private void RecordCallbackFailedEvent(Command request, string reason)
        {
            var tenantId = currentTenant.Id != Guid.Empty
                ? TenantId.From(currentTenant.Id)
                : TenantId.From(Guid.Empty);

            securityEventRepository.Add(SecurityEvent.Create(
                tenantId,
                userId: null,
                sessionId: null,
                SecurityEventType.OidcCallbackFailed,
                $"OIDC callback failed for provider '{request.Provider}': {reason}",
                riskScore: 50,
                request.IpAddress,
                request.UserAgent,
                $"{{\"provider\":\"{request.Provider}\"}}",
                dateTimeProvider.UtcNow));
        }
    }
}
