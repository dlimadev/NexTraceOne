using Ardalis.GuardClauses;
using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.Errors;
using NexTraceOne.Identity.Domain.ValueObjects;

namespace NexTraceOne.Identity.Application.Features.ChangePassword;

/// <summary>
/// Feature: ChangePassword — altera a senha do usuário autenticado (self-service).
///
/// Regras de negócio:
/// - Exige confirmação da senha atual antes de definir a nova.
/// - Usuários federados (sem senha local) não podem usar este endpoint — retorna InvalidCredentials.
/// - Toda alteração de senha gera SecurityEvent para trilha de auditoria (LGPD, ISO 27001).
/// - Falhas de verificação da senha atual também são registradas para detecção de anomalias.
/// </summary>
public static class ChangePassword
{
    /// <summary>Comando de alteração de senha do usuário.</summary>
    public sealed record Command(string CurrentPassword, string NewPassword) : ICommand;

    /// <summary>Valida a entrada de alteração de senha.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CurrentPassword).NotEmpty();
            RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(128);
        }
    }

    /// <summary>
    /// Handler que verifica a senha atual e define a nova.
    /// Gera SecurityEvent em caso de sucesso e falha para rastreabilidade completa.
    /// </summary>
    public sealed class Handler(
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ISecurityEventRepository securityEventRepository,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated || !Guid.TryParse(currentUser.Id, out var userId))
            {
                return IdentityErrors.NotAuthenticated();
            }

            var user = await userRepository.GetByIdAsync(UserId.From(userId), cancellationToken);
            if (user is null)
            {
                return IdentityErrors.UserNotFound(userId);
            }

            if (user.PasswordHash is null)
            {
                // Usuário federado sem senha local — não pode usar este fluxo
                return IdentityErrors.InvalidCredentials();
            }

            if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash.Value))
            {
                // Registra tentativa falha para detecção de anomalias e possível brute force
                RecordPasswordChangeFailedEvent(user.Id);
                return IdentityErrors.CurrentPasswordInvalid();
            }

            user.SetPassword(HashedPassword.FromHash(passwordHasher.Hash(request.NewPassword)));

            // Registra evento de sucesso para trilha de auditoria obrigatória
            RecordPasswordChangedEvent(user.Id);

            return Unit.Value;
        }

        /// <summary>
        /// Registra evento de alteração de senha bem-sucedida.
        /// Score de risco moderado (10) porque é uma ação legítima e esperada,
        /// mas relevante para auditoria de conformidade.
        /// </summary>
        private void RecordPasswordChangedEvent(UserId userId)
        {
            var tenantId = currentTenant.Id != Guid.Empty
                ? TenantId.From(currentTenant.Id)
                : TenantId.From(Guid.Empty);

            securityEventRepository.Add(SecurityEvent.Create(
                tenantId,
                userId,
                sessionId: null,
                SecurityEventType.PasswordChanged,
                $"Password changed successfully by user '{userId.Value}' (self-service).",
                riskScore: 10,
                ipAddress: null,
                userAgent: null,
                metadataJson: null,
                dateTimeProvider.UtcNow));
        }

        /// <summary>
        /// Registra tentativa falha de alteração de senha.
        /// Score de risco elevado (40) porque pode indicar acesso não autorizado
        /// tentando alterar a senha de outro usuário ou ataque de força bruta.
        /// </summary>
        private void RecordPasswordChangeFailedEvent(UserId userId)
        {
            var tenantId = currentTenant.Id != Guid.Empty
                ? TenantId.From(currentTenant.Id)
                : TenantId.From(Guid.Empty);

            securityEventRepository.Add(SecurityEvent.Create(
                tenantId,
                userId,
                sessionId: null,
                SecurityEventType.PasswordChangeFailed,
                $"Password change failed for user '{userId.Value}' — current password verification failed.",
                riskScore: 40,
                ipAddress: null,
                userAgent: null,
                metadataJson: null,
                dateTimeProvider.UtcNow));
        }
    }
}
