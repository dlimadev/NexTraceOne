using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.RegisterIdeClient;

/// <summary>
/// Feature: RegisterIdeClient — regista um novo cliente IDE para uso governado.
/// Valida o tipo de cliente e cria o registo de autorização.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class RegisterIdeClient
{
    /// <summary>Comando de registo de cliente IDE.</summary>
    public sealed record Command(
        string ClientType,
        string? ClientVersion,
        string? DeviceIdentifier) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de registo.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ClientType).NotEmpty();
        }
    }

    /// <summary>Handler que regista o cliente IDE.</summary>
    public sealed class Handler(
        IAiIdeClientRegistrationRepository clientRegistrationRepository,
        ICurrentUser currentUser) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!Enum.TryParse<AIClientType>(request.ClientType, ignoreCase: true, out var clientType)
                || (clientType != AIClientType.VsCode && clientType != AIClientType.VisualStudio))
            {
                return AiGovernanceErrors.InvalidIdeClientType(request.ClientType);
            }

            var registration = AIIDEClientRegistration.Register(
                currentUser.Id,
                currentUser.Name,
                clientType,
                request.ClientVersion,
                request.DeviceIdentifier);

            await clientRegistrationRepository.AddAsync(registration, cancellationToken);

            return new Response(
                registration.Id.Value,
                registration.UserId,
                registration.ClientType.ToString(),
                registration.IsActive);
        }
    }

    /// <summary>Resposta do registo de cliente IDE.</summary>
    public sealed record Response(
        Guid RegistrationId,
        string UserId,
        string ClientType,
        bool IsActive);
}
