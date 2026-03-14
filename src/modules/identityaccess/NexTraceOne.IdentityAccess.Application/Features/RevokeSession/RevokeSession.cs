using Ardalis.GuardClauses;
using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.Errors;

namespace NexTraceOne.Identity.Application.Features.RevokeSession;

/// <summary>
/// Feature: RevokeSession — revoga uma sessão autenticada existente.
/// </summary>
public static class RevokeSession
{
    /// <summary>Comando de revogação de sessão.</summary>
    public sealed record Command(Guid SessionId) : ICommand;

    /// <summary>Valida a entrada de revogação de sessão.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.SessionId).NotEmpty();
        }
    }

    /// <summary>Handler que marca a sessão como revogada.</summary>
    public sealed class Handler(
        ISessionRepository sessionRepository,
        NexTraceOne.BuildingBlocks.Application.Abstractions.IDateTimeProvider dateTimeProvider) : ICommandHandler<Command>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var session = await sessionRepository.GetByIdAsync(SessionId.From(request.SessionId), cancellationToken);
            if (session is null)
            {
                return IdentityErrors.SessionNotFound(request.SessionId);
            }

            session.Revoke(dateTimeProvider.UtcNow);
            return Unit.Value;
        }
    }
}
