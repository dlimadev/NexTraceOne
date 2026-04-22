using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetAgentPlanStatus;

/// <summary>
/// Feature: GetAgentPlanStatus — retorna o estado detalhado de um plano de execução agentic.
/// Inclui todos os passos com status, tokens e informações de aprovação.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class GetAgentPlanStatus
{
    /// <summary>Query para obter o estado de um plano agentic.</summary>
    public sealed record Query(Guid PlanId, Guid TenantId) : IQuery<Response>;

    /// <summary>Handler que retorna o plano com todos os detalhes dos passos.</summary>
    public sealed class Handler(
        IAgentExecutionPlanRepository planRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken ct)
        {
            Guard.Against.Null(request);

            var plan = await planRepository.GetByIdAsync(
                AgentExecutionPlanId.From(request.PlanId), ct);

            if (plan is null)
                return AiGovernanceErrors.AgentExecutionNotFound(request.PlanId.ToString());

            var steps = plan.Steps.Select(s => new StepDetail(
                StepIndex: s.StepIndex,
                Name: s.Name,
                StepType: s.StepType.ToString(),
                Status: s.Status.ToString(),
                RequiresApproval: s.RequiresApproval,
                InputJson: s.InputJson,
                OutputJson: s.OutputJson,
                TokensConsumed: s.TokensConsumed,
                ModelUsed: s.ModelUsed,
                DurationMs: s.DurationMs,
                ApprovedBy: s.ApprovedBy,
                ApprovedAt: s.ApprovedAt,
                ErrorMessage: s.ErrorMessage)).ToList();

            return new Response(
                PlanId: plan.Id.Value,
                Description: plan.Description,
                RequestedBy: plan.RequestedBy,
                Status: plan.PlanStatus.ToString(),
                CorrelationId: plan.CorrelationId,
                MaxTokenBudget: plan.MaxTokenBudget,
                ConsumedTokens: plan.ConsumedTokens,
                RequiresApproval: plan.RequiresApproval,
                ApprovedBy: plan.ApprovedBy,
                ApprovedAt: plan.ApprovedAt,
                StartedAt: plan.StartedAt,
                CompletedAt: plan.CompletedAt,
                ErrorMessage: plan.ErrorMessage,
                Steps: steps);
        }
    }

    /// <summary>Detalhe de um passo no plano.</summary>
    public sealed record StepDetail(
        int StepIndex,
        string Name,
        string StepType,
        string Status,
        bool RequiresApproval,
        string InputJson,
        string? OutputJson,
        int TokensConsumed,
        string? ModelUsed,
        long? DurationMs,
        string? ApprovedBy,
        DateTimeOffset? ApprovedAt,
        string? ErrorMessage);

    /// <summary>Resposta do estado detalhado do plano agentic.</summary>
    public sealed record Response(
        Guid PlanId,
        string Description,
        string RequestedBy,
        string Status,
        string CorrelationId,
        int MaxTokenBudget,
        int ConsumedTokens,
        bool RequiresApproval,
        string? ApprovedBy,
        DateTimeOffset? ApprovedAt,
        DateTimeOffset? StartedAt,
        DateTimeOffset? CompletedAt,
        string? ErrorMessage,
        IReadOnlyList<StepDetail> Steps);
}
