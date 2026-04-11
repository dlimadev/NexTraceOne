using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.IdentityAccess.Application.Features.ResetPassword;

/// <summary>
/// Feature: ResetPassword — efectua o reset de password usando token recebido por email.
/// Stub controlado: retorna erro claro até a infraestrutura de tokens estar implementada.
/// </summary>
public static class ResetPassword
{
    public sealed record Command(string Token, string NewPassword) : ICommand<Response>, IPublicRequest;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Token).NotEmpty().MaximumLength(512);
            RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(128);
        }
    }

    public sealed record Response(bool Success);

    internal sealed class Handler : ICommandHandler<Command, Response>
    {
        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Token de reset ainda não é gerado nem persistido.
            // Quando o módulo de Notificações suportar envio de tokens:
            // 1. Validar token contra store de tokens de reset
            // 2. Verificar expiração
            // 3. Actualizar password do utilizador
            // 4. Invalidar token usado
            return Task.FromResult<Result<Response>>(
                Error.Validation("password.reset.token_invalid",
                    "Reset token infrastructure not yet implemented"));
        }
    }
}
