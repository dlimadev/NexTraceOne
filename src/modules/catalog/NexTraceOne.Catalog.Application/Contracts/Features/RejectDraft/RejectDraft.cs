using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.RejectDraft;

/// <summary>
/// Feature: RejectDraft — rejeita um draft de contrato após revisão, retornando para edição.
/// Cria um registro de revisão com a decisão e comentário do revisor.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class RejectDraft
{
    /// <summary>Comando de rejeição de draft de contrato.</summary>
    public sealed record Command(
        Guid DraftId,
        string RejectedBy,
        string? Comment = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de rejeição.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.DraftId).NotEmpty();
            RuleFor(x => x.RejectedBy).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Comment).MaximumLength(2000).When(x => x.Comment is not null);
        }
    }

    /// <summary>
    /// Handler que rejeita um draft e cria o registro de revisão.
    /// Carrega o draft pelo id, delega a rejeição ao método de domínio Reject,
    /// cria ContractReview com decisão Rejected, e persiste as alterações.
    /// </summary>
    public sealed class Handler(
        IContractDraftRepository draftRepository,
        IContractReviewRepository reviewRepository,
        IUnitOfWork unitOfWork,
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

            var rejectResult = draft.Reject(request.RejectedBy, now);
            if (rejectResult.IsFailure)
                return rejectResult.Error;

            var review = ContractReview.Create(
                draft.Id,
                request.RejectedBy,
                ReviewDecision.Rejected,
                request.Comment ?? string.Empty,
                now);

            reviewRepository.Add(review);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(draft.Id.Value);
        }
    }

    /// <summary>Resposta da rejeição de draft de contrato.</summary>
    public sealed record Response(Guid DraftId);
}
