using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Promotion.Application.Abstractions;
using NexTraceOne.Promotion.Domain.Entities;
using NexTraceOne.Promotion.Domain.Errors;

namespace NexTraceOne.Promotion.Application.Features.ApprovePromotion;

/// <summary>
/// Feature: ApprovePromotion — aprova manualmente uma solicitação de promoção em estado InEvaluation.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ApprovePromotion
{
    /// <summary>Comando para aprovação de uma solicitação de promoção.</summary>
    public sealed record Command(
        Guid PromotionRequestId,
        string ApprovedBy,
        string? Comment) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de aprovação de promoção.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.PromotionRequestId).NotEmpty();
            RuleFor(x => x.ApprovedBy).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Comment).MaximumLength(4000).When(x => x.Comment is not null);
        }
    }

    /// <summary>Handler que aprova uma PromotionRequest após verificação dos gates obrigatórios.</summary>
    public sealed class Handler(
        IPromotionRequestRepository requestRepository,
        IPromotionGateRepository gateRepository,
        IGateEvaluationRepository evaluationRepository,
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

            var requiredGates = await gateRepository.ListRequiredByEnvironmentIdAsync(
                promotionRequest.TargetEnvironmentId, cancellationToken);

            if (requiredGates.Count > 0)
            {
                var evaluations = await evaluationRepository.ListByRequestIdAsync(
                    promotionRequest.Id, cancellationToken);

                var passedGateIds = evaluations
                    .Where(e => e.Passed)
                    .Select(e => e.PromotionGateId.Value)
                    .ToHashSet();

                var unpassed = requiredGates.FirstOrDefault(g => !passedGateIds.Contains(g.Id.Value));
                if (unpassed is not null)
                    return PromotionErrors.GateNotPassed(unpassed.GateName);
            }

            var now = dateTimeProvider.UtcNow;
            var approveResult = promotionRequest.Approve(now);
            if (approveResult.IsFailure)
                return approveResult.Error;

            requestRepository.Update(promotionRequest);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(promotionRequest.Id.Value, promotionRequest.Status.ToString(), promotionRequest.CompletedAt);
        }
    }

    /// <summary>Resposta da aprovação da solicitação de promoção.</summary>
    public sealed record Response(Guid PromotionRequestId, string Status, DateTimeOffset? CompletedAt);
}
