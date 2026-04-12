using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.IdentityAccess.Application.Features.AcceptInvitation;

/// <summary>
/// Feature: AcceptInvitation — aceita um convite e cria conta de utilizador.
/// Stub controlado: infraestrutura de convites ainda não implementada.
/// </summary>
public static class AcceptInvitation
{
    public sealed record Command(string Token, string Password) : ICommand<Response>, IPublicRequest;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Token).NotEmpty().MaximumLength(512);
            RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        }
    }

    public sealed record Response(bool Accepted);

    internal sealed class Handler : ICommandHandler<Command, Response>
    {
        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Infraestrutura de convites (validação de token, criação de conta,
            // associação ao tenant) ainda não implementada.
            return Task.FromResult<Result<Response>>(
                Error.NotFound("invitation.not_found",
                    "Invitation not found or expired"));
        }
    }
}
