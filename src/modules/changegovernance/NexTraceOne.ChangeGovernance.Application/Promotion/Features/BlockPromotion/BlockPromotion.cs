using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Promotion.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Enums;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Errors;

namespace NexTraceOne.ChangeGovernance.Application.Promotion.Features.BlockPromotion;

/// <summary>
/// Feature: BlockPromotion — bloqueia uma solicitação de promoção por regra de governança.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class BlockPromotion
{
    /// <summary>Comando para bloqueio de uma solicitação de promoção.</summary>
    public sealed record Command(
        Guid PromotionRequestId,
        string BlockedBy,
        string Reason) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de bloqueio de promoção.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.PromotionRequestId).NotEmpty();
            RuleFor(x => x.BlockedBy).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Reason).NotEmpty().MaximumLength(4000);
        }
    }

    /// <summary>Handler que bloqueia uma PromotionRequest por regra de governança.</summary>
    public sealed class Handler(
        IPromotionRequestRepository requestRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var promotionRequest = await requestRepository.GetByIdAsync(
                PromotionRequestId.From(request.PromotionRequestId), cancellationToken);
            if (promotionRequest is null)
                return PromotionErrors.RequestNotFound(request.PromotionRequestId.ToString());

            var now = dateTimeProvider.UtcNow;

            // Transiciona para InEvaluation se ainda estiver Pending (Block requer InEvaluation)
            if (promotionRequest.Status == PromotionStatus.Pending)
            {
                var startResult = promotionRequest.StartEvaluation();
                if (startResult.IsFailure)
                    return startResult.Error;
            }

            var blockResult = promotionRequest.Block(now);
            if (blockResult.IsFailure)
                return blockResult.Error;

            requestRepository.Update(promotionRequest);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(promotionRequest.Id.Value, promotionRequest.Status.ToString(), promotionRequest.CompletedAt);
        }
    }

    /// <summary>Resposta do bloqueio da solicitação de promoção.</summary>
    public sealed record Response(Guid PromotionRequestId, string Status, DateTimeOffset? CompletedAt);
}
