using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Application.Features.ForgotPassword;

/// <summary>
/// Feature: ForgotPassword — inicia o fluxo de recuperação de password.
/// Retorna sempre sucesso para prevenir enumeração de emails.
/// A entrega do email de reset é responsabilidade futura do módulo de Notificações.
/// Auditoria de reset será registada quando o token for efectivamente utilizado.
/// </summary>
public static class ForgotPassword
{
    public sealed record Command(string Email) : ICommand<Response>, IPublicRequest;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        }
    }

    public sealed record Response(bool Accepted);

    internal sealed class Handler(
        IUserRepository userRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var email = Email.Create(request.Email);
            // Verificar existência para futuro envio de email de reset.
            // Não registar evento de auditoria aqui — será registado quando o token
            // for utilizado em ResetPassword, evitando confusão semântica nos logs.
            _ = await userRepository.GetByEmailAsync(email, cancellationToken);

            // Sempre retorna sucesso para prevenir enumeração de emails
            return new Response(true);
        }
    }
}

