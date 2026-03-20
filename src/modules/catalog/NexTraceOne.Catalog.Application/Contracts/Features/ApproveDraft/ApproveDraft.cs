using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ApproveDraft;

/// <summary>
/// Feature: ApproveDraft — aprova um draft de contrato após revisão.
/// Cria um registro de revisão com a decisão e comentário do aprovador.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ApproveDraft
{
    /// <summary>Comando de aprovação de draft de contrato.</summary>
    public sealed record Command(
        Guid DraftId,
        string ApprovedBy,
        string? Comment = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de aprovação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.DraftId).NotEmpty();
            RuleFor(x => x.ApprovedBy).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Comment).MaximumLength(2000).When(x => x.Comment is not null);
        }
    }

    /// <summary>
    /// Handler que aprova um draft e cria o registro de revisão.
    /// Carrega o draft pelo id, delega a aprovação ao método de domínio Approve,
    /// cria ContractReview com decisão Approved, e persiste as alterações.
    /// </summary>
    public sealed class Handler(
        IContractDraftRepository draftRepository,
        IContractReviewRepository reviewRepository,
        IContractsUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var draft = await draftRepository.GetByIdAsync(
                ContractDraftId.From(request.DraftId), cancellationToken);

            if (draft is null)
                return ContractsErrors.DraftNotFound(request.DraftId.ToString());

            var now = dateTimeProvider.UtcNow;

            var approveResult = draft.Approve(request.ApprovedBy, now);
            if (approveResult.IsFailure)
                return approveResult.Error;

            var review = ContractReview.Create(
                draft.Id,
                request.ApprovedBy,
                ReviewDecision.Approved,
                request.Comment ?? string.Empty,
                now);

            reviewRepository.Add(review);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(draft.Id.Value);
        }
    }

    /// <summary>Resposta da aprovação de draft de contrato.</summary>
    public sealed record Response(Guid DraftId);
}
