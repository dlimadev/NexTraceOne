using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Errors;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;

namespace NexTraceOne.IdentityAccess.Application.Features.RequestBreakGlass;

/// <summary>
/// Feature: RequestBreakGlass — solicita acesso emergencial com ativação imediata.
///
/// Fluxo:
/// 1. Valida justificativa e elegibilidade do solicitante.
/// 2. Verifica limite trimestral (máximo 3 usos antes de escalar para revisão).
/// 3. Se o utilizador tem MFA habilitado, verifica o código step-up obrigatório.
/// 4. Ativa acesso imediatamente com janela padrão de 2 horas.
/// 5. Registra evento de segurança para notificação aos administradores.
/// 6. Retorna dados da ativação para confirmação visual.
///
/// Segurança: toda ação durante o período Break Glass é rastreada com o BreakGlassRequestId.
/// Step-up MFA: utilizadores com MFA habilitado devem fornecer código TOTP válido para activar.
/// </summary>
public static class RequestBreakGlass
{
    /// <summary>Comando para solicitação de acesso emergencial.</summary>
    public sealed record Command(
        string Justification,
        string? MfaCode = null,
        string? IpAddress = null,
        string? UserAgent = null) : ICommand<Response>;

    /// <summary>Valida a entrada da solicitação Break Glass.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Justification).NotEmpty().MinimumLength(20).MaximumLength(2000);
        }
    }

    /// <summary>Handler que processa a solicitação de acesso emergencial.</summary>
    public sealed class Handler(
        IBreakGlassRepository breakGlassRepository,
        IUserRepository userRepository,
        ISecurityEventRepository securityEventRepository,
        ISecurityEventTracker securityEventTracker,
        INotificationModule notificationModule,
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
                    // Emite evento de step-up necessário e bloqueia a operação
                    var stepUpEvent = SecurityEvent.Create(
                        tenantId, userId, sessionId: null,
                        SecurityEventType.StepUpMfaRequired,
                        $"Step-up MFA required for Break Glass activation by user {userId.Value}.",
                        riskScore: 60,
                        request.IpAddress, request.UserAgent,
                        System.Text.Json.JsonSerializer.Serialize(new { operation = "RequestBreakGlass" }),
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
                        $"Step-up MFA denied for Break Glass activation by user {userId.Value} — invalid code.",
                        riskScore: 70,
                        request.IpAddress, request.UserAgent,
                        System.Text.Json.JsonSerializer.Serialize(new { operation = "RequestBreakGlass" }),
                        now);
                    securityEventRepository.Add(deniedEvent);
                    securityEventTracker.Track(deniedEvent);
                    return IdentityErrors.MfaCodeInvalid();
                }
            }

            // Calcula início do trimestre atual para verificação de quota
            var quarterStart = new DateTimeOffset(
                now.Year, ((now.Month - 1) / 3) * 3 + 1, 1,
                0, 0, 0, TimeSpan.Zero);

            var usageCount = await breakGlassRepository.CountQuarterlyUsageAsync(userId, quarterStart, cancellationToken);

            if (usageCount >= BreakGlassRequest.QuarterlyUsageLimit)
                return IdentityErrors.BreakGlassQuotaExceeded(userId.Value, usageCount);

            var breakGlass = BreakGlassRequest.Create(
                userId,
                tenantId,
                request.Justification,
                request.IpAddress ?? "unknown",
                request.UserAgent ?? "unknown",
                now);

            breakGlassRepository.Add(breakGlass);

            // Registra evento de segurança de alta prioridade
            var securityEvent = SecurityEvent.Create(
                tenantId,
                userId,
                sessionId: null,
                SecurityEventType.BreakGlassActivated,
                $"Break glass access activated by user {userId.Value}. Justification: {request.Justification[..Math.Min(100, request.Justification.Length)]}",
                riskScore: 90,
                request.IpAddress,
                request.UserAgent,
                metadataJson: System.Text.Json.JsonSerializer.Serialize(new
                {
                    breakGlassRequestId = breakGlass.Id.Value,
                    justificationPreview = request.Justification[..Math.Min(100, request.Justification.Length)],
                    expiresAt = breakGlass.ExpiresAt,
                    mfaVerified = user.MfaEnabled
                }),
                now);

            securityEventRepository.Add(securityEvent);
            securityEventTracker.Track(securityEvent);

            await notificationModule.SubmitAsync(new NotificationRequest
            {
                EventType = "BreakGlassActivated",
                Category = "Security",
                Severity = "Critical",
                Title = "Break-glass access activated",
                Message = $"Emergency break-glass access was activated by user {userId.Value}. Review immediately.",
                SourceModule = "Identity",
                SourceEntityType = nameof(BreakGlassRequest),
                SourceEntityId = breakGlass.Id.Value.ToString(),
                ActionUrl = $"/security/break-glass/{breakGlass.Id.Value}",
                RequiresAction = true,
                TenantId = tenantId.Value,
                RecipientUserIds = [userId.Value],
                PayloadJson = securityEvent.MetadataJson,
                SourceEventId = securityEvent.Id.Value.ToString()
            }, cancellationToken);

            return new Response(
                breakGlass.Id.Value,
                breakGlass.ExpiresAt!.Value,
                usageCount + 1,
                BreakGlassRequest.QuarterlyUsageLimit);
        }
    }

    /// <summary>Resposta da ativação de acesso emergencial.</summary>
    public sealed record Response(
        Guid RequestId,
        DateTimeOffset ExpiresAt,
        int QuarterlyUsageCount,
        int QuarterlyUsageLimit);
}
