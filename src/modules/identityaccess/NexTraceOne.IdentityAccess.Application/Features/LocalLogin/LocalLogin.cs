using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Errors;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Application.Features.LocalLogin;

/// <summary>
/// Feature: LocalLogin — autentica um usuário local com email e senha.
///
/// Fluxo:
/// 1. Valida credenciais (email + senha) contra o repositório.
/// 2. Verifica estado de bloqueio da conta.
/// 3. Resolve o TenantMembership e Role do usuário.
/// 4. Cria sessão e emite tokens JWT via <see cref="ILoginSessionCreator"/>.
/// 5. Registra eventos de auditoria via <see cref="ISecurityAuditRecorder"/>.
///
/// Delegação de responsabilidades:
/// - Criação de sessão → <see cref="ILoginSessionCreator"/> (SRP/DIP).
/// - Eventos de auditoria → <see cref="ISecurityAuditRecorder"/> (SRP/DIP).
/// - Resolução de membership → <see cref="ILoginResponseBuilder"/> (DRY/DIP).
/// - Construção de resposta → <see cref="ILoginResponseBuilder"/> (DRY/DIP).
/// </summary>
public static class LocalLogin
{
    /// <summary>Comando de autenticação local.</summary>
    public sealed record Command(
        string Email,
        string Password,
        string? IpAddress = null,
        string? UserAgent = null) : ICommand<LoginResponse>, IPublicRequest;

    /// <summary>Valida a entrada do login local.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        }
    }

    /// <summary>
    /// Handler que autentica um usuário local e emite tokens.
    ///
    /// Orquestra o fluxo de autenticação local delegando responsabilidades
    /// específicas para serviços injetados via DI:
    /// - ILoginSessionCreator para criação de sessão (DIP).
    /// - ISecurityAuditRecorder para registro de eventos de segurança (DIP).
    /// - ILoginResponseBuilder para resolução de membership e construção de resposta (DIP).
    /// - IMfaChallengeTokenService para emissão de token de desafio MFA quando necessário.
    ///
    /// Fluxo com MFA:
    /// 1. Valida credenciais (igual ao fluxo sem MFA).
    /// 2. Se user.MfaEnabled = true, emite desafio MFA e retorna resposta parcial (MfaRequired = true).
    /// 3. Cliente deve chamar POST /auth/mfa/verify com o challenge token + código TOTP.
    /// 4. VerifyMfaChallenge emite a sessão completa após validação do código.
    /// </summary>
    public sealed class Handler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider,
        ISecurityAuditRecorder auditRecorder,
        ILoginSessionCreator sessionCreator,
        ILoginResponseBuilder responseBuilder,
        IMfaChallengeTokenService mfaChallengeTokenService) : ICommandHandler<Command, LoginResponse>
    {
        public async Task<Result<LoginResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var user = await userRepository.GetByEmailAsync(Email.Create(request.Email), cancellationToken);
            if (user is null || !user.IsActive || user.PasswordHash is null)
            {
                RecordAuthFailure(request, null, "Invalid credentials or user inactive.");
                return IdentityErrors.InvalidCredentials();
            }

            if (user.IsLocked(dateTimeProvider.UtcNow))
            {
                RecordAuthFailure(request, user.Id, "Account locked due to excessive failed attempts.");
                return IdentityErrors.AccountLocked(user.LockoutEnd);
            }

            if (!passwordHasher.Verify(request.Password, user.PasswordHash.Value))
            {
                user.RegisterFailedLogin(dateTimeProvider.UtcNow);

                if (user.IsLocked(dateTimeProvider.UtcNow))
                {
                    auditRecorder.RecordAccountLocked(
                        auditRecorder.ResolveTenantIdForAudit(),
                        user.Id, request.IpAddress, request.UserAgent);
                    return IdentityErrors.AccountLocked(user.LockoutEnd);
                }

                RecordAuthFailure(request, user.Id, "Invalid password.");
                return IdentityErrors.InvalidCredentials();
            }

            var membership = await responseBuilder.ResolveMembershipAsync(user.Id, cancellationToken);

            if (membership is null)
            {
                return IdentityErrors.TenantMembershipNotFound(user.Id.Value, responseBuilder.CurrentTenantId);
            }

            // MFA enforcement: se o utilizador tem MFA habilitado, emitir desafio em vez de tokens completos.
            // O cliente deve completar o desafio via POST /auth/mfa/verify antes de receber tokens de acesso.
            if (user.MfaEnabled)
            {
                var challengeToken = mfaChallengeTokenService.Issue(
                    user.Id.Value,
                    dateTimeProvider.UtcNow.AddMinutes(5));

                auditRecorder.RecordStepUpMfaRequired(
                    membership.TenantId,
                    user.Id,
                    "login",
                    request.IpAddress,
                    request.UserAgent);

                return new LoginResponse(
                    AccessToken: string.Empty,
                    RefreshToken: string.Empty,
                    ExpiresIn: 0,
                    User: new UserResponse(user.Id.Value, user.Email.Value, user.FullName.Value, Guid.Empty, string.Empty, []),
                    MfaRequired: true,
                    MfaChallengeToken: challengeToken);
            }

            var role = await roleRepository.GetByIdAsync(membership.RoleId, cancellationToken);
            if (role is null)
            {
                return IdentityErrors.RoleNotFound(membership.RoleId.Value);
            }

            user.RegisterSuccessfulLogin(dateTimeProvider.UtcNow);

            // Delega criação de sessão para ILoginSessionCreator (SRP/DIP)
            var (_, refreshToken) = sessionCreator.CreateSession(
                user.Id, request.IpAddress, request.UserAgent);

            // Delega registro de evento de sucesso para ISecurityAuditRecorder (SRP/DIP)
            auditRecorder.RecordAuthenticationSuccess(
                membership.TenantId, user.Id,
                request.IpAddress, request.UserAgent);

            return responseBuilder.CreateLoginResponse(user, membership, role, refreshToken);
        }

        /// <summary>Registra evento de falha de autenticação delegando para ISecurityAuditRecorder.</summary>
        private void RecordAuthFailure(Command request, UserId? userId, string reason)
        {
            auditRecorder.RecordAuthenticationFailure(
                auditRecorder.ResolveTenantIdForAudit(),
                userId, reason, request.IpAddress, request.UserAgent);
        }
    }

    /// <summary>Resposta padronizada de autenticação.</summary>
    public sealed record LoginResponse(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn,
        UserResponse User,
        bool MfaRequired = false,
        string? MfaChallengeToken = null);

    /// <summary>Resumo do usuário autenticado incluído na resposta de login.</summary>
    public sealed record UserResponse(
        Guid Id,
        string Email,
        string FullName,
        Guid TenantId,
        string RoleName,
        IReadOnlyList<string> Permissions);
}
