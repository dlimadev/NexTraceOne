using Ardalis.GuardClauses;

using FluentValidation;

using MediatR;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Errors;

namespace NexTraceOne.IdentityAccess.Application.Features.ActivateUser;

/// <summary>
/// Feature: ActivateUser — reativa um usuário previamente desativado.
/// </summary>
public static class ActivateUser
{
    /// <summary>Comando de ativação de usuário.</summary>
    public sealed record Command(Guid UserId, Guid TenantId) : ICommand;

    /// <summary>Valida a entrada de ativação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
        }
    }

    /// <summary>Handler que reativa o usuário e regista evento de segurança.</summary>
    public sealed class Handler(
        IUserRepository userRepository,
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

            user.Activate();

            var securityEvent = SecurityEvent.Create(
                TenantId.From(request.TenantId),
                user.Id,
                sessionId: null,
                SecurityEventType.UserActivated,
                $"User '{user.Email.Value}' reactivated.",
                riskScore: 15,
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
