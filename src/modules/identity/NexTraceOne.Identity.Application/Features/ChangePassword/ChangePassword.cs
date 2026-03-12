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
/// Feature: ChangePassword — altera a senha do usuário autenticado.
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

    /// <summary>Handler que verifica a senha atual e define a nova.</summary>
    public sealed class Handler(
        ICurrentUser currentUser,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher) : ICommandHandler<Command>
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
                return IdentityErrors.InvalidCredentials();
            }

            if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash.Value))
            {
                return IdentityErrors.CurrentPasswordInvalid();
            }

            user.SetPassword(HashedPassword.FromHash(passwordHasher.Hash(request.NewPassword)));
            return Unit.Value;
        }
    }
}
