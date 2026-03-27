using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Errors;

using LocalLoginFeature = NexTraceOne.IdentityAccess.Application.Features.LocalLogin.LocalLogin;

namespace NexTraceOne.IdentityAccess.Application.Features.VerifyMfaChallenge;

/// <summary>
/// Feature: VerifyMfaChallenge — segundo passo do fluxo de autenticação com MFA.
///
/// Fluxo:
/// 1. LocalLogin valida as credenciais; se user.MfaEnabled, emite ChallengeToken em vez de tokens completos.
/// 2. Cliente recebe MfaRequired = true com o ChallengeToken e apresenta a UI de MFA.
/// 3. Cliente chama este handler com o ChallengeToken + código TOTP do autenticador.
/// 4. Handler valida o token de desafio, verifica o código TOTP, cria sessão e emite tokens completos.
///
/// Segurança:
/// - O ChallengeToken expira em 5 minutos (janela curta de ataque).
/// - O token é assinado com HMAC-SHA256 e não pode ser adulterado.
/// - Falhas de MFA são auditadas como SecurityEvent.MfaChallengeFailed.
/// - Sucessos são auditados como SecurityEvent.MfaChallengeSucceeded.
/// </summary>
public static class VerifyMfaChallenge
{
    /// <summary>Comando para verificação do desafio MFA.</summary>
    public sealed record Command(
        string ChallengeToken,
        string Code,
        string? IpAddress = null,
        string? UserAgent = null) : ICommand<LocalLoginFeature.LoginResponse>, IPublicRequest;

    /// <summary>Valida a entrada da verificação MFA.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ChallengeToken).NotEmpty();
            RuleFor(x => x.Code).NotEmpty().Length(6).Matches("^[0-9]{6}$")
                .WithMessage("MFA code must be a 6-digit numeric code.");
        }
    }

    /// <summary>
    /// Handler que valida o desafio MFA e emite sessão completa em caso de sucesso.
    /// </summary>
    public sealed class Handler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IDateTimeProvider dateTimeProvider,
        ISecurityAuditRecorder auditRecorder,
        ILoginSessionCreator sessionCreator,
        ILoginResponseBuilder responseBuilder,
        IMfaChallengeTokenService mfaChallengeTokenService,
        ITotpVerifier totpVerifier) : ICommandHandler<Command, LocalLoginFeature.LoginResponse>
    {
        public async Task<Result<LocalLoginFeature.LoginResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // 1. Validar e descodificar o token de desafio
            if (!mfaChallengeTokenService.TryValidate(request.ChallengeToken, out var userId))
                return IdentityErrors.MfaChallengeExpiredOrInvalid();

            // 2. Carregar o utilizador e verificar estado
            var user = await userRepository.GetByIdAsync(UserId.From(userId), cancellationToken);
            if (user is null || !user.IsActive || !user.MfaEnabled || string.IsNullOrWhiteSpace(user.MfaSecret))
                return IdentityErrors.MfaChallengeExpiredOrInvalid();

            var tenantId = auditRecorder.ResolveTenantIdForAudit();

            // 3. Verificar código TOTP
            if (!totpVerifier.Verify(user.MfaSecret, request.Code))
            {
                auditRecorder.RecordMfaChallengeFailed(tenantId, user.Id, request.IpAddress, request.UserAgent);
                return IdentityErrors.MfaCodeInvalid();
            }

            // 4. Resolver membership e role
            var membership = await responseBuilder.ResolveMembershipAsync(user.Id, cancellationToken);
            if (membership is null)
                return IdentityErrors.TenantMembershipNotFound(user.Id.Value, responseBuilder.CurrentTenantId);

            var role = await roleRepository.GetByIdAsync(membership.RoleId, cancellationToken);
            if (role is null)
                return IdentityErrors.RoleNotFound(membership.RoleId.Value);

            // 5. Criar sessão completa e emitir tokens
            user.RegisterSuccessfulLogin(dateTimeProvider.UtcNow);
            var (_, refreshToken) = sessionCreator.CreateSession(user.Id, request.IpAddress, request.UserAgent);

            auditRecorder.RecordMfaChallengeSuccess(membership.TenantId, user.Id, request.IpAddress, request.UserAgent);

            return responseBuilder.CreateLoginResponse(user, membership, role, refreshToken);
        }
    }
}
