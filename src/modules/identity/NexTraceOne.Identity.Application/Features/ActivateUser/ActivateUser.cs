using Ardalis.GuardClauses;
using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Identity.Application.Abstractions;
using NexTraceOne.Identity.Domain.Entities;
using NexTraceOne.Identity.Domain.Errors;

namespace NexTraceOne.Identity.Application.Features.ActivateUser;

/// <summary>
/// Feature: ActivateUser — reativa um usuário previamente desativado.
/// </summary>
public static class ActivateUser
{
    /// <summary>Comando de ativação de usuário.</summary>
    public sealed record Command(Guid UserId) : ICommand;

    /// <summary>Valida a entrada de ativação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty();
        }
    }

    /// <summary>Handler que reativa o usuário.</summary>
    public sealed class Handler(IUserRepository userRepository) : ICommandHandler<Command>
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
            return Unit.Value;
        }
    }
}
