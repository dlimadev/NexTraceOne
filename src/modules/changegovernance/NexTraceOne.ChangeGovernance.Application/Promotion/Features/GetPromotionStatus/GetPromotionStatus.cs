using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Promotion.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Errors;

namespace NexTraceOne.ChangeGovernance.Application.Promotion.Features.GetPromotionStatus;

/// <summary>
/// Feature: GetPromotionStatus — retorna o status atual e detalhes de uma solicitação de promoção.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetPromotionStatus
{
    /// <summary>Query para obtenção do status de uma solicitação de promoção.</summary>
    public sealed record Query(Guid PromotionRequestId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de status de promoção.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.PromotionRequestId).NotEmpty();
        }
    }

    /// <summary>Handler que retorna o status atual de uma PromotionRequest com suas avaliações.</summary>
    public sealed class Handler(
        IPromotionRequestRepository requestRepository,
        IGateEvaluationRepository evaluationRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var promotionRequest = await requestRepository.GetByIdAsync(
                PromotionRequestId.From(request.PromotionRequestId), cancellationToken);
            if (promotionRequest is null)
                return PromotionErrors.RequestNotFound(request.PromotionRequestId.ToString());

            var evaluations = await evaluationRepository.ListByRequestIdAsync(
                promotionRequest.Id, cancellationToken);

            int passedCount = evaluations.Count(e => e.Passed);

            return new Response(
                promotionRequest.Id.Value,
                promotionRequest.ReleaseId,
                promotionRequest.Status.ToString(),
                promotionRequest.RequestedBy,
                promotionRequest.RequestedAt,
                promotionRequest.CompletedAt,
                promotionRequest.Justification,
                evaluations.Count,
                passedCount);
        }
    }

    /// <summary>Resposta com o status atual da solicitação de promoção.</summary>
    public sealed record Response(
        Guid PromotionRequestId,
        Guid ReleaseId,
        string Status,
        string RequestedBy,
        DateTimeOffset RequestedAt,
        DateTimeOffset? CompletedAt,
        string? Justification,
        int TotalEvaluations,
        int PassedEvaluations);
}
