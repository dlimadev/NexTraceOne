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

    /// <summary>Handler que autentica um usuário local e emite tokens.</summary>
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
                    RecordAccountLocked(request, user.Id);
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

            var refreshToken = jwtTokenGenerator.GenerateRefreshToken();
            var session = Session.Create(
                user.Id,
                RefreshTokenHash.Create(refreshToken),
                dateTimeProvider.UtcNow.AddDays(30),
                request.IpAddress ?? "unknown",
                request.UserAgent ?? "unknown");

            sessionRepository.Add(session);

            RecordAuthSuccess(request, user.Id, membership.TenantId);

            return IdentityFeatureSupport.CreateLoginResponse(
                user,
                membership,
                role,
                jwtTokenGenerator,
                refreshToken);
        }

        /// <summary>Registra evento de autenticação bem-sucedida para trilha de auditoria.</summary>
        private void RecordAuthSuccess(Command request, UserId userId, TenantId tenantId)
        {
            securityEventRepository.Add(SecurityEvent.Create(
                tenantId,
                userId,
                sessionId: null,
                SecurityEventType.AuthenticationSucceeded,
                $"Local authentication succeeded for user '{userId.Value}'.",
                riskScore: 0,
                request.IpAddress,
                request.UserAgent,
                metadataJson: null,
                dateTimeProvider.UtcNow));
        }

        /// <summary>Registra evento de falha de autenticação para detecção de anomalias.</summary>
        private void RecordAuthFailure(Command request, UserId? userId, string reason)
        {
            var tenantId = currentTenant.Id != Guid.Empty
                ? TenantId.From(currentTenant.Id)
                : TenantId.From(Guid.Empty);

            securityEventRepository.Add(SecurityEvent.Create(
                tenantId,
                userId,
                sessionId: null,
                SecurityEventType.AuthenticationFailed,
                $"Local authentication failed: {reason}",
                riskScore: 30,
                request.IpAddress,
                request.UserAgent,
                metadataJson: null,
                dateTimeProvider.UtcNow));
        }

        /// <summary>Registra evento de bloqueio de conta por tentativas excessivas.</summary>
        private void RecordAccountLocked(Command request, UserId userId)
        {
            var tenantId = currentTenant.Id != Guid.Empty
                ? TenantId.From(currentTenant.Id)
                : TenantId.From(Guid.Empty);

            securityEventRepository.Add(SecurityEvent.Create(
                tenantId,
                userId,
                sessionId: null,
                SecurityEventType.AccountLocked,
                $"Account locked for user '{userId.Value}' due to excessive failed login attempts.",
                riskScore: 70,
                request.IpAddress,
                request.UserAgent,
                metadataJson: null,
                dateTimeProvider.UtcNow));
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
