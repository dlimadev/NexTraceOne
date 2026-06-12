using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Promotion.Abstractions;
using NexTraceOne.ChangeGovernance.Contracts.IntegrationEvents;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Errors;

using IReleaseRepository = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions.IReleaseRepository;

namespace NexTraceOne.ChangeGovernance.Application.Promotion.Features.ApprovePromotion;

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
        IReleaseRepository releaseRepository,
        IDeploymentEnvironmentRepository environmentRepository,
        IPromotionUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IEventBus eventBus) : ICommandHandler<Command, Response>
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
            var approveResult = promotionRequest.Approve(now, request.ApprovedBy, request.Comment);
            if (approveResult.IsFailure)
                return approveResult.Error;

            requestRepository.Update(promotionRequest);
            await unitOfWork.CommitAsync(cancellationToken);

            // Publica o evento de integração após o commit — consumido pelo módulo
            // de notificações para avisar o owner do serviço sobre a promoção.
            var release = await releaseRepository.GetByIdAsync(
                ReleaseId.From(promotionRequest.ReleaseId), cancellationToken);
            var targetEnvironment = await environmentRepository.GetByIdAsync(
                promotionRequest.TargetEnvironmentId, cancellationToken);

            await eventBus.PublishAsync(
                new PromotionCompletedIntegrationEvent(
                    promotionRequest.Id.Value,
                    release?.ServiceName ?? string.Empty,
                    targetEnvironment?.Name ?? string.Empty,
                    OwnerUserId: null),
                cancellationToken);

            return new Response(promotionRequest.Id.Value, promotionRequest.Status.ToString(), promotionRequest.CompletedAt);
        }
    }

    /// <summary>Resposta da aprovação da solicitação de promoção.</summary>
    public sealed record Response(Guid PromotionRequestId, string Status, DateTimeOffset? CompletedAt);
}
