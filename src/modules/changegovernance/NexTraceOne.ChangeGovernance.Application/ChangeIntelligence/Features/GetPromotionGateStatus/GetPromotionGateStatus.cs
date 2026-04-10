using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetPromotionGateStatus;

/// <summary>
/// Feature: GetPromotionGateStatus — retorna o estado atual de um gate de promoção
/// e as suas avaliações mais recentes.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetPromotionGateStatus
{
    /// <summary>Query de consulta do estado de um gate de promoção.</summary>
    public sealed record Query(Guid GateId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.GateId).NotEmpty();
        }
    }

    /// <summary>Handler que retorna o estado de um gate de promoção e avaliações recentes.</summary>
    public sealed class Handler(
        IPromotionGateRepository gateRepository,
        IPromotionGateEvaluationRepository evaluationRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var gateId = PromotionGateId.From(request.GateId);
            var gate = await gateRepository.GetByIdAsync(gateId, cancellationToken);
            if (gate is null)
                return PromotionGateErrors.GateNotFound(request.GateId.ToString());

            var evaluations = await evaluationRepository.ListByGateAsync(gateId, cancellationToken);

            return new Response(
                gate.Id.Value,
                gate.Name,
                gate.Description,
                gate.EnvironmentFrom,
                gate.EnvironmentTo,
                gate.IsActive,
                gate.BlockOnFailure,
                gate.CreatedAt,
                evaluations.Select(e => new EvaluationSummary(
                    e.Id.Value,
                    e.ChangeId,
                    e.Result,
                    e.EvaluatedAt,
                    e.EvaluatedBy)).ToList());
        }
    }

    /// <summary>Resposta com o estado do gate de promoção e avaliações recentes.</summary>
    public sealed record Response(
        Guid GateId,
        string Name,
        string? Description,
        string EnvironmentFrom,
        string EnvironmentTo,
        bool IsActive,
        bool BlockOnFailure,
        DateTimeOffset CreatedAt,
        IReadOnlyList<EvaluationSummary> RecentEvaluations);

    /// <summary>Resumo de uma avaliação de gate.</summary>
    public sealed record EvaluationSummary(
        Guid EvaluationId,
        string ChangeId,
        GateEvaluationResult Result,
        DateTimeOffset EvaluatedAt,
        string? EvaluatedBy);
}
