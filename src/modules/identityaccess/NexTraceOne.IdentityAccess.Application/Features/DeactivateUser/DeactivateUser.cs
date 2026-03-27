using Ardalis.GuardClauses;

using FluentValidation;

using MediatR;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Errors;

namespace NexTraceOne.IdentityAccess.Application.Features.DeactivateUser;

/// <summary>
/// Feature: DeactivateUser — desativa um usuário impedindo novos logins.
/// </summary>
public static class DeactivateUser
{
    /// <summary>Comando de desativação de usuário.</summary>
    public sealed record Command(Guid UserId, Guid TenantId) : ICommand;

    /// <summary>Valida a entrada de desativação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
        }
    }

    /// <summary>Handler que desativa o usuário, revoga a sessão ativa e regista evento de segurança.</summary>
    public sealed class Handler(
        IUserRepository userRepository,
        ISessionRepository sessionRepository,
        ISecurityEventRepository securityEventRepository,
        ISecurityEventTracker securityEventTracker,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var user = await userRepository.GetByIdAsync(UserId.From(request.UserId), cancellationToken);
            if (user is null)
            {
                return IdentityErrors.UserNotFound(request.UserId);
            }

            user.Deactivate();

            var session = await sessionRepository.GetActiveByUserIdAsync(user.Id, cancellationToken);
            session?.Revoke(dateTimeProvider.UtcNow);

            var securityEvent = SecurityEvent.Create(
                TenantId.From(request.TenantId),
                user.Id,
                sessionId: null,
                SecurityEventType.UserDeactivated,
                $"User '{user.Email.Value}' deactivated.",
                riskScore: 30,
                ipAddress: null,
                userAgent: null,
                metadataJson: null,
                dateTimeProvider.UtcNow);
            securityEventRepository.Add(securityEvent);
            securityEventTracker.Track(securityEvent);

            return Unit.Value;
        }
    }
}
