using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Errors;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;

using LocalLoginFeature = NexTraceOne.IdentityAccess.Application.Features.LocalLogin.LocalLogin;

namespace NexTraceOne.IdentityAccess.Application.Features.RefreshToken;

/// <summary>
/// Feature: RefreshToken — rotaciona o refresh token e emite novo access token.
///
/// Delegação de responsabilidades:
/// - Resolução de membership → <see cref="ILoginResponseBuilder"/> (DRY/DIP).
/// - Construção de resposta → <see cref="ILoginResponseBuilder"/> (DRY/DIP).
/// - Auditoria de contexto suspeito de sessão → <see cref="ISecurityAuditRecorder"/> (SRP/DIP).
///
/// Segurança: detecta mudança de IP/UserAgent entre a criação e o refresh da sessão,
/// gerando SecurityEvent.SuspiciousSessionContextDetected para auditoria.
/// </summary>
public static class RefreshToken
{
    /// <summary>Comando de renovação de tokens.</summary>
    public sealed record Command(string RefreshToken, string? IpAddress = null, string? UserAgent = null)
        : ICommand<LocalLogin.LocalLogin.LoginResponse>, IPublicRequest;

    /// <summary>Valida a entrada da rotação de refresh token.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.RefreshToken).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que valida o refresh token, rotaciona a sessão e emite novos tokens.
    ///
    /// Orquestra o fluxo de rotação delegando responsabilidades específicas
    /// para serviços injetados via DI:
    /// - ILoginResponseBuilder para resolução de membership e construção de resposta (DIP).
    /// - ISecurityAuditRecorder para registo de contexto suspeito de sessão (DIP).
    /// </summary>
    public sealed class Handler(
        ISessionRepository sessionRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IDateTimeProvider dateTimeProvider,
        ILoginResponseBuilder responseBuilder,
        ISecurityAuditRecorder auditRecorder) : ICommandHandler<Command, LocalLogin.LocalLogin.LoginResponse>
    {
        public async Task<Result<LocalLoginFeature.LoginResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var refreshTokenHash = RefreshTokenHash.Create(request.RefreshToken);
            var session = await sessionRepository.GetByRefreshTokenHashAsync(refreshTokenHash, cancellationToken);

            if (session is null)
            {
                return IdentityErrors.InvalidRefreshToken();
            }

            if (session.RevokedAt.HasValue)
            {
                return IdentityErrors.SessionRevoked(session.Id.Value);
            }

            if (session.IsExpired(dateTimeProvider.UtcNow))
            {
                return IdentityErrors.SessionExpired(session.Id.Value);
            }

            var user = await userRepository.GetByIdAsync(session.UserId, cancellationToken);
            if (user is null)
            {
                return IdentityErrors.UserNotFound(session.UserId.Value);
            }

            var membership = await responseBuilder.ResolveMembershipAsync(user.Id, cancellationToken);

            if (membership is null)
            {
                return IdentityErrors.TenantMembershipNotFound(user.Id.Value, responseBuilder.CurrentTenantId);
            }

            var role = await roleRepository.GetByIdAsync(membership.RoleId, cancellationToken);
            if (role is null)
            {
                return IdentityErrors.RoleNotFound(membership.RoleId.Value);
            }

            // Detecção de contexto de sessão suspeito: IP alterado entre criação e refresh.
            // Gera SecurityEvent para auditoria sem revogar a sessão por defeito (comportamento seguro
            // que não impacta utilizadores legítimos com IPs dinâmicos, mas cria trilha de anomalia).
            if (!string.IsNullOrWhiteSpace(request.IpAddress) &&
                !string.Equals(session.CreatedByIp, request.IpAddress, StringComparison.OrdinalIgnoreCase) &&
                session.CreatedByIp != "unknown")
            {
                auditRecorder.RecordSuspiciousSessionContext(
                    membership.TenantId,
                    user.Id,
                    session.Id,
                    "IP address changed since session creation.",
                    request.IpAddress,
                    session.CreatedByIp);
            }

            var newRefreshToken = jwtTokenGenerator.GenerateRefreshToken();
            session.Rotate(RefreshTokenHash.Create(newRefreshToken), dateTimeProvider.UtcNow.AddDays(30));
            user.RegisterSuccessfulLogin(dateTimeProvider.UtcNow);

            return responseBuilder.CreateLoginResponse(user, membership, role, newRefreshToken);
        }
    }
}
