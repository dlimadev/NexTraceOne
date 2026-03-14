using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Promotion.Application.Abstractions;
using NexTraceOne.Promotion.Domain.Entities;

namespace NexTraceOne.Promotion.Application.Features.GetGateEvaluation;

/// <summary>
/// Feature: GetGateEvaluation — retorna as avaliações de gates de uma solicitação de promoção.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetGateEvaluation
{
    /// <summary>Item de avaliação de gate individual.</summary>
    public sealed record GateEvaluationItem(
        Guid EvaluationId,
        Guid GateId,
        bool Passed,
        string EvaluatedBy,
        string? Details,
        string? OverrideJustification,
        DateTimeOffset EvaluatedAt);

    /// <summary>Query para obtenção das avaliações de gates de uma solicitação de promoção.</summary>
    public sealed record Query(Guid PromotionRequestId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de avaliação de gates.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.PromotionRequestId).NotEmpty();
        }
    }

    /// <summary>Handler que retorna as avaliações de gates de uma solicitação de promoção.</summary>
    public sealed class Handler(
        IGateEvaluationRepository evaluationRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var evaluations = await evaluationRepository.ListByRequestIdAsync(
                PromotionRequestId.From(request.PromotionRequestId), cancellationToken);

            var items = evaluations.Select(e => new GateEvaluationItem(
                e.Id.Value,
                e.PromotionGateId.Value,
                e.Passed,
                e.EvaluatedBy,
                e.EvaluationDetails,
                e.OverrideJustification,
                e.EvaluatedAt)).ToList();

            return new Response(request.PromotionRequestId, items);
        }
    }

    /// <summary>Resposta com as avaliações de gates de uma solicitação de promoção.</summary>
    public sealed record Response(Guid PromotionRequestId, IReadOnlyList<GateEvaluationItem> Evaluations);
}
