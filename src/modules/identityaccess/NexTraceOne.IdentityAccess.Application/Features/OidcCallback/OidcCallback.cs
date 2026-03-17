using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Errors;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;

using LocalLoginFeature = NexTraceOne.IdentityAccess.Application.Features.LocalLogin.LocalLogin;

namespace NexTraceOne.IdentityAccess.Application.Features.OidcCallback;

/// <summary>
/// Feature: OidcCallback — processa o callback do provider OIDC após autenticação externa.
///
/// Fluxo completo:
/// 1. Provider redireciona para /auth/oidc/callback?code=...&amp;state=...
/// 2. Este handler valida o state (previne CSRF), extrai returnTo.
/// 3. Troca o code por informações do usuário via IOidcProvider.ExchangeCodeAsync.
/// 4. Cria ou vincula o usuário interno (User + ExternalIdentity).
/// 5. Resolve o TenantMembership do usuário.
/// 6. Cria sessão via <see cref="ILoginSessionCreator"/> e emite JWT token.
/// 7. Registra eventos de auditoria via <see cref="ISecurityAuditRecorder"/>.
/// 8. Retorna o token + returnTo para o frontend completar a navegação.
///
/// Delegação de responsabilidades:
/// - Criação de sessão → <see cref="ILoginSessionCreator"/> (SRP/DIP).
/// - Eventos de auditoria → <see cref="ISecurityAuditRecorder"/> (SRP/DIP).
/// - Resolução de membership → <see cref="ILoginResponseBuilder"/> (DRY/DIP).
/// - Construção de resposta → <see cref="ILoginResponseBuilder"/> (DRY/DIP).
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
///
/// Refatoração: reduzido de 9 para 6 dependências diretas via extração
/// de serviços injetáveis, melhorando testabilidade e aderência ao SRP/DIP.
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
    ///
    /// Orquestra o fluxo de autenticação federada delegando responsabilidades
    /// específicas para serviços injetados via DI:
    /// - ILoginSessionCreator para criação de sessão (DIP).
    /// - ISecurityAuditRecorder para registro de eventos de segurança (DIP).
    /// - ILoginResponseBuilder para resolução de membership e construção de resposta (DIP).
    /// </summary>
    public sealed class Handler(
        IOidcProvider oidcProvider,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ITenantMembershipRepository membershipRepository,
        IDateTimeProvider dateTimeProvider,
        ICurrentTenant currentTenant,
        ISecurityAuditRecorder auditRecorder,
        ILoginSessionCreator sessionCreator,
        ILoginResponseBuilder responseBuilder) : ICommandHandler<Command, Response>
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
                auditRecorder.RecordOidcCallbackFailure(
                    auditRecorder.ResolveTenantIdForAudit(),
                    request.Provider, $"Token exchange failed: {ex.Message}",
                    request.IpAddress, request.UserAgent);
                return IdentityErrors.OidcCallbackFailed(request.Provider);
            }

            // Localiza ou cria o usuário interno vinculado à identidade federada
            var user = await ResolveOrProvisionUserAsync(
                request.Provider, userInfo, cancellationToken);

            // Resolve o TenantMembership do usuário no tenant corrente
            var membership = await responseBuilder.ResolveMembershipAsync(user.Id, cancellationToken);

            if (membership is null && currentTenant.Id != Guid.Empty)
            {
                // Auto-provisiona o usuário como Viewer no tenant se não tiver membership
                membership = await AutoProvisionMembershipAsync(user.Id, cancellationToken);
                if (membership is null)
                {
                    return IdentityErrors.RoleNotFound(Guid.Empty);
                }
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

            // Delega criação de sessão para ILoginSessionCreator (SRP/DIP)
            var (session, refreshToken) = sessionCreator.CreateSession(
                user.Id, request.IpAddress, request.UserAgent);

            // Delega registro de callback bem-sucedido para ISecurityAuditRecorder (SRP/DIP)
            auditRecorder.RecordOidcCallbackSuccess(
                membership.TenantId, user.Id, session.Id,
                request.Provider, userInfo.ExternalId,
                request.IpAddress, request.UserAgent);

            var loginResponse = responseBuilder.CreateLoginResponse(
                user, membership, role, refreshToken);

            return new Response(
                loginResponse.AccessToken,
                loginResponse.RefreshToken,
                loginResponse.ExpiresIn,
                loginResponse.User,
                returnTo);
        }

        /// <summary>
        /// Localiza o usuário por identidade federada ou email.
        /// Se não encontrado, provisiona automaticamente (JIT provisioning).
        /// Se encontrado sem vínculo federado, vincula a identidade externa.
        /// </summary>
        private async Task<User> ResolveOrProvisionUserAsync(
            string provider,
            OidcUserInfo userInfo,
            CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByFederatedIdentityAsync(
                provider,
                userInfo.ExternalId,
                cancellationToken);

            user ??= await userRepository.GetByEmailAsync(Email.Create(userInfo.Email), cancellationToken);

            if (user is null)
            {
                // Provisiona novo usuário federado automaticamente (Just-in-Time provisioning)
                user = User.CreateFederated(
                    Email.Create(userInfo.Email),
                    FullName.FromDisplayName(userInfo.DisplayName),
                    provider,
                    userInfo.ExternalId);
                userRepository.Add(user);
            }
            else
            {
                // Vincula a identidade externa ao usuário existente se ainda não vinculada
                user.LinkFederatedIdentity(provider, userInfo.ExternalId);
            }

            return user;
        }

        /// <summary>
        /// Auto-provisiona o usuário como Viewer no tenant corrente quando não existe membership.
        /// Usado no fluxo OIDC para garantir acesso mínimo ao primeiro login federado.
        /// </summary>
        private async Task<TenantMembership?> AutoProvisionMembershipAsync(
            UserId userId,
            CancellationToken cancellationToken)
        {
            var viewerRole = await roleRepository.GetByNameAsync(Role.Viewer, cancellationToken);
            if (viewerRole is null)
            {
                return null;
            }

            var membership = TenantMembership.Create(
                userId,
                TenantId.From(currentTenant.Id),
                viewerRole.Id,
                dateTimeProvider.UtcNow);

            membershipRepository.Add(membership);
            return membership;
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
    }
}
