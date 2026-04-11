using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.IdentityAccess.Application.Features.ActivateAccount;

/// <summary>
/// Feature: ActivateAccount — activa uma conta nova usando token de activação.
/// Stub controlado: retorna erro claro até a infraestrutura de tokens de activação estar implementada.
/// </summary>
public static class ActivateAccount
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

    public sealed record Response(bool Activated);

    internal sealed class Handler : ICommandHandler<Command, Response>
    {
        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Token de activação ainda não é gerado nem persistido.
            // Quando implementado:
            // 1. Validar token contra store de tokens de activação
            // 2. Verificar expiração
            // 3. Activar utilizador e definir password
            // 4. Invalidar token usado
            return Task.FromResult<Result<Response>>(
                Error.Validation("account.activation.token_invalid",
                    "Account activation token infrastructure not yet implemented"));
        }
    }
}
