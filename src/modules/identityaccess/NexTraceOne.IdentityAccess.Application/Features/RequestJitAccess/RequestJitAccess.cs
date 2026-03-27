using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Errors;

namespace NexTraceOne.IdentityAccess.Application.Features.RequestJitAccess;

/// <summary>
/// Feature: RequestJitAccess — solicita acesso privilegiado temporário (Just-in-Time).
///
/// O solicitante indica a permissão desejada, o escopo específico e a justificativa.
/// A solicitação fica pendente até aprovação por um responsável ou expiração do prazo.
///
/// Step-up MFA: utilizadores com MFA habilitado devem fornecer código TOTP válido para solicitar.
/// </summary>
public static class RequestJitAccess
{
    /// <summary>Comando para solicitação de acesso JIT.</summary>
    public sealed record Command(
        string PermissionCode,
        string Scope,
        string Justification,
        string? MfaCode = null) : ICommand<Response>;

    /// <summary>Valida a entrada da solicitação JIT.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.PermissionCode).NotEmpty().MaximumLength(256);
            RuleFor(x => x.Scope).NotEmpty().MaximumLength(1000);
            RuleFor(x => x.Justification).NotEmpty().MinimumLength(10).MaximumLength(2000);
        }
    }

    /// <summary>Handler que cria a solicitação de acesso JIT.</summary>
    public sealed class Handler(
        IJitAccessRepository jitAccessRepository,
        IUserRepository userRepository,
        ISecurityEventRepository securityEventRepository,
        ISecurityEventTracker securityEventTracker,
        ITotpVerifier totpVerifier,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (string.IsNullOrWhiteSpace(currentUser.Id))
                return IdentityErrors.NotAuthenticated();

            var userId = UserId.From(Guid.Parse(currentUser.Id));
            var tenantId = TenantId.From(currentTenant.Id);
            var now = dateTimeProvider.UtcNow;

            // Step-up MFA: verificar utilizador e enforçar MFA se habilitado
            var user = await userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null || !user.IsActive)
                return IdentityErrors.UserNotFound(userId.Value);

            if (user.MfaEnabled)
            {
                if (string.IsNullOrWhiteSpace(request.MfaCode))
                {
                    var stepUpEvent = SecurityEvent.Create(
                        tenantId, userId, sessionId: null,
                        SecurityEventType.StepUpMfaRequired,
                        $"Step-up MFA required for JIT access request by user {userId.Value}.",
                        riskScore: 50,
                        ipAddress: null, userAgent: null,
                        System.Text.Json.JsonSerializer.Serialize(new
                        {
                            operation = "RequestJitAccess",
                            permissionCode = request.PermissionCode
                        }),
                        now);
                    securityEventRepository.Add(stepUpEvent);
                    securityEventTracker.Track(stepUpEvent);
                    return IdentityErrors.MfaStepUpRequired();
                }

                if (!totpVerifier.Verify(user.MfaSecret!, request.MfaCode))
                {
                    var deniedEvent = SecurityEvent.Create(
                        tenantId, userId, sessionId: null,
                        SecurityEventType.MfaStepUpDenied,
                        $"Step-up MFA denied for JIT access request by user {userId.Value} — invalid code.",
                        riskScore: 60,
                        ipAddress: null, userAgent: null,
                        System.Text.Json.JsonSerializer.Serialize(new
                        {
                            operation = "RequestJitAccess",
                            permissionCode = request.PermissionCode
                        }),
                        now);
                    securityEventRepository.Add(deniedEvent);
                    securityEventTracker.Track(deniedEvent);
                    return IdentityErrors.MfaCodeInvalid();
                }
            }

            var jitRequest = JitAccessRequest.Create(
                userId,
                tenantId,
                request.PermissionCode,
                request.Scope,
                request.Justification,
                now);

            jitAccessRepository.Add(jitRequest);

            var securityEvent = SecurityEvent.Create(
                tenantId,
                userId,
                sessionId: null,
                SecurityEventType.JitAccessRequested,
                $"JIT access requested for permission '{request.PermissionCode}' by user {userId.Value}.",
                riskScore: 40,
                ipAddress: null,
                userAgent: null,
                metadataJson: System.Text.Json.JsonSerializer.Serialize(new
                {
                    jitAccessRequestId = jitRequest.Id.Value,
                    permissionCode = request.PermissionCode,
                    scope = request.Scope,
                    approvalDeadline = jitRequest.ApprovalDeadline,
                    mfaVerified = user.MfaEnabled
                }),
                now);

            securityEventRepository.Add(securityEvent);
            securityEventTracker.Track(securityEvent);

            return new Response(
                jitRequest.Id.Value,
                jitRequest.ApprovalDeadline);
        }
    }

    /// <summary>Resposta da criação de solicitação JIT.</summary>
    public sealed record Response(
        Guid RequestId,
        DateTimeOffset ApprovalDeadline);
}
