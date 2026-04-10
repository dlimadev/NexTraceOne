using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.CreateContractNegotiation;

/// <summary>
/// Feature: CreateContractNegotiation — cria uma negociação cross-team de contrato.
/// Regista a negociação com estado Draft e persiste para rastreabilidade.
/// Estrutura VSA: Command + Validator + Handler + Response em arquivo único.
/// </summary>
public static class CreateContractNegotiation
{
    /// <summary>Comando para criar uma nova negociação de contrato.</summary>
    public sealed record Command(
        Guid? ContractId,
        Guid ProposedByTeamId,
        string ProposedByTeamName,
        string Title,
        string Description,
        DateTimeOffset? Deadline,
        string Participants,
        int ParticipantCount,
        string? ProposedContractSpec,
        string InitiatedByUserId) : ICommand<Response>;

    /// <summary>Valida os parâmetros do comando de criação de negociação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ProposedByTeamId).NotEmpty();
            RuleFor(x => x.ProposedByTeamName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
            RuleFor(x => x.Participants).NotEmpty();
            RuleFor(x => x.ParticipantCount).GreaterThan(0);
            RuleFor(x => x.InitiatedByUserId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ContractId)
                .Must(id => id != Guid.Empty)
                .When(x => x.ContractId.HasValue)
                .WithMessage("ContractId must not be empty when provided.");
        }
    }

    /// <summary>
    /// Handler que cria e persiste uma nova negociação cross-team de contrato.
    /// </summary>
    public sealed class Handler(
        IContractNegotiationRepository repository,
        IContractsUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var negotiation = ContractNegotiation.Create(
                contractId: request.ContractId,
                proposedByTeamId: request.ProposedByTeamId,
                proposedByTeamName: request.ProposedByTeamName,
                title: request.Title,
                description: request.Description,
                deadline: request.Deadline,
                participants: request.Participants,
                participantCount: request.ParticipantCount,
                proposedContractSpec: request.ProposedContractSpec,
                initiatedByUserId: request.InitiatedByUserId,
                createdAt: clock.UtcNow);

            await repository.AddAsync(negotiation, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                negotiation.Id.Value,
                negotiation.ContractId,
                negotiation.ProposedByTeamId,
                negotiation.ProposedByTeamName,
                negotiation.Title,
                negotiation.Status,
                negotiation.ParticipantCount,
                negotiation.CreatedAt);
        }
    }

    /// <summary>Resposta da criação de negociação de contrato.</summary>
    public sealed record Response(
        Guid NegotiationId,
        Guid? ContractId,
        Guid ProposedByTeamId,
        string ProposedByTeamName,
        string Title,
        NegotiationStatus Status,
        int ParticipantCount,
        DateTimeOffset CreatedAt);
}
