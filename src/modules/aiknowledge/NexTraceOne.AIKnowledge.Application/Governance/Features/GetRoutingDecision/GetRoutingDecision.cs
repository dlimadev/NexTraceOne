using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetRoutingDecision;

/// <summary>
/// Feature: GetRoutingDecision — obtém detalhes de uma decisão de roteamento por ID.
/// Fornece explicabilidade completa sobre por que um modelo/caminho foi selecionado.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class GetRoutingDecision
{
    /// <summary>Query de obtenção de decisão de roteamento por ID.</summary>
    public sealed record Query(Guid DecisionId) : IQuery<Response>;

    /// <summary>Handler que obtém decisão de roteamento com metadados completos.</summary>
    public sealed class Handler(
        IAiRoutingDecisionRepository decisionRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            Guard.Against.Default(request.DecisionId);

            var decision = await decisionRepository.GetByIdAsync(
                AIRoutingDecisionId.From(request.DecisionId),
                cancellationToken);

            if (decision is null)
                return AiGovernanceErrors.RoutingDecisionNotFound(request.DecisionId.ToString());

            return new Response(
                decision.Id.Value,
                decision.CorrelationId,
                decision.Persona,
                decision.UseCaseType.ToString(),
                decision.ClientType,
                decision.SelectedPath.ToString(),
                decision.SelectedModelName,
                decision.SelectedProvider,
                decision.IsInternalModel,
                decision.AppliedStrategyId,
                decision.AppliedPolicyName,
                decision.EscalationReason.ToString(),
                decision.Rationale,
                decision.EstimatedCostClass,
                decision.ConfidenceLevel.ToString(),
                decision.SelectedSources,
                decision.SourceWeightingSummary,
                decision.DecidedAt);
        }
    }

    /// <summary>Resposta com detalhes completos da decisão de roteamento.</summary>
    public sealed record Response(
        Guid DecisionId,
        string CorrelationId,
        string Persona,
        string UseCaseType,
        string ClientType,
        string SelectedPath,
        string SelectedModelName,
        string SelectedProvider,
        bool IsInternalModel,
        Guid? AppliedStrategyId,
        string? AppliedPolicyName,
        string EscalationReason,
        string Rationale,
        string EstimatedCostClass,
        string ConfidenceLevel,
        string SelectedSources,
        string SourceWeightingSummary,
        DateTimeOffset DecidedAt);
}
