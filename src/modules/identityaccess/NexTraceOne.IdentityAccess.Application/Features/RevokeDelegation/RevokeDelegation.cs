using Ardalis.GuardClauses;

using FluentValidation;

using MediatR;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Errors;

namespace NexTraceOne.IdentityAccess.Application.Features.RevokeDelegation;

/// <summary>
/// Feature: RevokeDelegation — revoga uma delegação ativa antecipadamente.
/// Pode ser executado pelo delegante, pelo delegatário ou por um administrador.
/// </summary>
public static class RevokeDelegation
{
    /// <summary>Comando para revogação de delegação.</summary>
    public sealed record Command(Guid DelegationId) : ICommand;

    /// <summary>Valida a entrada da revogação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.DelegationId).NotEmpty();
        }
    }

    /// <summary>Handler que processa a revogação de delegação.</summary>
    public sealed class Handler(
        IDelegationRepository delegationRepository,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (string.IsNullOrWhiteSpace(currentUser.Id))
                return IdentityErrors.NotAuthenticated();

            var delegation = await delegationRepository.GetByIdAsync(
                DelegationId.From(request.DelegationId), cancellationToken);

            if (delegation is null)
                return IdentityErrors.DelegationNotFound(request.DelegationId);

            delegation.Revoke(UserId.From(Guid.Parse(currentUser.Id)), dateTimeProvider.UtcNow);

            return Unit.Value;
        }
    }
}
