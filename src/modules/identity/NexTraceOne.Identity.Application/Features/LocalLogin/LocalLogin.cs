using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Application.Features;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.Errors;
using NexTraceOne.Identity.Domain.ValueObjects;

namespace NexTraceOne.Identity.Application.Features.LocalLogin;

/// <summary>
/// Feature: LocalLogin — autentica um usuário local com email e senha.
///
/// Fluxo:
/// 1. Valida credenciais (email + senha) contra o repositório.
/// 2. Verifica estado de bloqueio da conta.
/// 3. Resolve o TenantMembership e Role do usuário.
/// 4. Cria sessão e emite tokens JWT via <see cref="LoginSessionCreator"/>.
/// 5. Registra eventos de auditoria via <see cref="SecurityAuditRecorder"/>.
///
/// Delegação de responsabilidades:
/// - Criação de sessão → <see cref="LoginSessionCreator"/> (SRP).
/// - Eventos de auditoria → <see cref="SecurityAuditRecorder"/> (SRP).
/// - Resolução de membership → <see cref="IdentityFeatureSupport"/> (DRY).
/// - Construção de resposta → <see cref="IdentityFeatureSupport"/> (DRY).
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
    /// específicas para serviços extraídos:
    /// - LoginSessionCreator para criação de sessão.
    /// - SecurityAuditRecorder para registro de eventos de segurança.
    /// </summary>
    public sealed class Handler(
        IUserRepository userRepository,
        ITenantMembershipRepository membershipRepository,
        IRoleRepository roleRepository,
        ISessionRepository sessionRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IDateTimeProvider dateTimeProvider,
        ICurrentTenant currentTenant,
        ISecurityEventRepository securityEventRepository) : ICommandHandler<Command, LoginResponse>
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
                    SecurityAuditRecorder.RecordAccountLocked(
                        securityEventRepository, dateTimeProvider,
                        SecurityAuditRecorder.ResolveTenantIdForAudit(currentTenant),
                        user.Id, request.IpAddress, request.UserAgent);
                    return IdentityErrors.AccountLocked(user.LockoutEnd);
                }

                RecordAuthFailure(request, user.Id, "Invalid password.");
                return IdentityErrors.InvalidCredentials();
            }

            var membership = await IdentityFeatureSupport.ResolveMembershipAsync(
                currentTenant,
                membershipRepository,
                user.Id,
                cancellationToken);

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

            // Delega criação de sessão para LoginSessionCreator (SRP)
            var (_, refreshToken) = LoginSessionCreator.CreateSession(
                jwtTokenGenerator, sessionRepository, dateTimeProvider,
                user.Id, request.IpAddress, request.UserAgent);

            // Delega registro de evento de sucesso para SecurityAuditRecorder (SRP)
            SecurityAuditRecorder.RecordAuthenticationSuccess(
                securityEventRepository, dateTimeProvider,
                membership.TenantId, user.Id,
                request.IpAddress, request.UserAgent);

            return IdentityFeatureSupport.CreateLoginResponse(
                user,
                membership,
                role,
                jwtTokenGenerator,
                refreshToken);
        }

        /// <summary>Registra evento de falha de autenticação delegando para SecurityAuditRecorder.</summary>
        private void RecordAuthFailure(Command request, UserId? userId, string reason)
        {
            SecurityAuditRecorder.RecordAuthenticationFailure(
                securityEventRepository, dateTimeProvider,
                SecurityAuditRecorder.ResolveTenantIdForAudit(currentTenant),
                userId, reason, request.IpAddress, request.UserAgent);
        }
    }

    /// <summary>Resposta padronizada de autenticação.</summary>
    public sealed record LoginResponse(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn,
        UserResponse User);

    /// <summary>Resumo do usuário autenticado incluído na resposta de login.</summary>
    public sealed record UserResponse(
        Guid Id,
        string Email,
        string FullName,
        Guid TenantId,
        string RoleName,
        IReadOnlyList<string> Permissions);
}
