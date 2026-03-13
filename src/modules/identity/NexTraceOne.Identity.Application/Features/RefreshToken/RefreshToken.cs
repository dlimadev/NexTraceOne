using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Identity.Application.Abstractions;
using LocalLoginFeature = NexTraceOne.Identity.Application.Features.LocalLogin.LocalLogin;
using NexTraceOne.Identity.Domain.Errors;
using NexTraceOne.Identity.Domain.ValueObjects;

namespace NexTraceOne.Identity.Application.Features.RefreshToken;

/// <summary>
/// Feature: RefreshToken — rotaciona o refresh token e emite novo access token.
///
/// Delegação de responsabilidades:
/// - Resolução de membership → <see cref="ILoginResponseBuilder"/> (DRY/DIP).
/// - Construção de resposta → <see cref="ILoginResponseBuilder"/> (DRY/DIP).
///
/// Refatoração: reduzido de 7 para 5 dependências diretas via serviços injetáveis.
/// </summary>
public static class RefreshToken
{
    /// <summary>Comando de renovação de tokens.</summary>
    public sealed record Command(string RefreshToken, string? IpAddress = null, string? UserAgent = null)
        : ICommand<LocalLoginFeature.LoginResponse>, IPublicRequest;

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
    /// </summary>
    public sealed class Handler(
        ISessionRepository sessionRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IDateTimeProvider dateTimeProvider,
        ILoginResponseBuilder responseBuilder) : ICommandHandler<Command, LocalLoginFeature.LoginResponse>
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
                return IdentityErrors.TenantMembershipNotFound(user.Id.Value, Guid.Empty);
            }

            var role = await roleRepository.GetByIdAsync(membership.RoleId, cancellationToken);
            if (role is null)
            {
                return IdentityErrors.RoleNotFound(membership.RoleId.Value);
            }

            var newRefreshToken = jwtTokenGenerator.GenerateRefreshToken();
            session.Rotate(RefreshTokenHash.Create(newRefreshToken), dateTimeProvider.UtcNow.AddDays(30));
            user.RegisterSuccessfulLogin(dateTimeProvider.UtcNow);

            return responseBuilder.CreateLoginResponse(user, membership, role, newRefreshToken);
        }
    }
}
