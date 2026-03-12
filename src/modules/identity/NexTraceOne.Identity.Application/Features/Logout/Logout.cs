using Ardalis.GuardClauses;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.Errors;

namespace NexTraceOne.Identity.Application.Features.Logout;

/// <summary>
/// Feature: Logout — revoga a sessão ativa do usuário autenticado.
/// </summary>
public static class Logout
{
    /// <summary>Comando de logout que revoga a sessão ativa do usuário atual.</summary>
    public sealed record Command : ICommand;

    /// <summary>Handler que revoga a sessão ativa do usuário autenticado.</summary>
    public sealed class Handler(
        ICurrentUser currentUser,
        ISessionRepository sessionRepository,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated || !Guid.TryParse(currentUser.Id, out var userId))
            {
                return IdentityErrors.NotAuthenticated();
            }

            var session = await sessionRepository.GetActiveByUserIdAsync(
                UserId.From(userId),
                cancellationToken);

            if (session is not null)
            {
                session.Revoke(dateTimeProvider.UtcNow);
            }

            return Unit.Value;
        }
    }
}
