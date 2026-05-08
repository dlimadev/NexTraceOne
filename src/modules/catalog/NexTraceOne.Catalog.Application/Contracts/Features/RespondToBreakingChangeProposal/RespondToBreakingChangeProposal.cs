using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.RespondToBreakingChangeProposal;

/// <summary>
/// Feature: RespondToBreakingChangeProposal — regista a resposta de um consumidor a uma proposta
/// de breaking change, transitando o estado da proposta para ConsumerResponded.
///
/// Transições válidas de entrada: ConsultationOpen → ConsumerResponded.
/// Se o estado já é ConsumerResponded, a resposta é registada sem nova transição.
///
/// Referência: CC-06, FUTURE-ROADMAP.md Wave A.2.
/// Ownership: módulo Catalog (Contracts).
/// </summary>
public static class RespondToBreakingChangeProposal
{
    public sealed record Command(
        Guid ProposalId,
        string ConsumerService,
        string? ResponseNote = null) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ProposalId).NotEmpty();
            RuleFor(x => x.ConsumerService).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ResponseNote).MaximumLength(2000).When(x => x.ResponseNote is not null);
        }
    }

    public sealed class Handler(
        IBreakingChangeProposalRepository repository,
        IContractsUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var proposalId = new BreakingChangeProposalId(request.ProposalId);
            var proposal = await repository.GetByIdAsync(proposalId, cancellationToken);
            if (proposal is null)
                return ContractsErrors.ProposalNotFound(request.ProposalId.ToString());

            if (proposal.Status is not (BreakingChangeProposalStatus.ConsultationOpen
                                     or BreakingChangeProposalStatus.ConsumerResponded))
                return ContractsErrors.ProposalNotOpenForResponse(
                    request.ProposalId.ToString(), proposal.Status.ToString());

            proposal.RecordConsumerResponse(clock.UtcNow);

            await repository.UpdateAsync(proposal, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                ProposalId: proposal.Id.Value,
                ConsumerService: request.ConsumerService,
                Status: proposal.Status.ToString(),
                RespondedAt: clock.UtcNow,
                ResponseNote: request.ResponseNote);
        }
    }

    public sealed record Response(
        Guid ProposalId,
        string ConsumerService,
        string Status,
        DateTimeOffset RespondedAt,
        string? ResponseNote);
}
