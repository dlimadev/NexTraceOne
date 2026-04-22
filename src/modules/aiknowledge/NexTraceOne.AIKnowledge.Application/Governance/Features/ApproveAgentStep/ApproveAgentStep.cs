using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ApproveAgentStep;

/// <summary>
/// Feature: ApproveAgentStep — aprova um passo de um plano agentic em WaitingApproval.
/// Retoma a execução do plano após aprovação humana (Human-in-the-Loop).
/// Estrutura VSA: Command + Handler + Response num único ficheiro.
/// </summary>
public static class ApproveAgentStep
{
    /// <summary>Comando de aprovação de passo de plano agentic.</summary>
    public sealed record Command(
        Guid PlanId,
        int StepIndex,
        string ApprovedBy,
        Guid TenantId) : ICommand<Response>;

    /// <summary>Handler que aprova o passo e retoma a execução do plano.</summary>
    public sealed class Handler(
        IAgentExecutionPlanRepository planRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken ct)
        {
            Guard.Against.Null(request);
            Guard.Against.NullOrWhiteSpace(request.ApprovedBy);

            var plan = await planRepository.GetByIdAsync(
                AgentExecutionPlanId.From(request.PlanId), ct);

            if (plan is null)
                return AiGovernanceErrors.AgentExecutionNotFound(request.PlanId.ToString());

            var step = plan.Steps.FirstOrDefault(s => s.StepIndex == request.StepIndex);
            if (step is null)
                return Error.NotFound(
                    "AgentPlan.StepNotFound",
                    "Step {0} not found in plan {1}.",
                    request.StepIndex.ToString(),
                    request.PlanId.ToString());

            if (!step.RequiresApproval)
                return Error.Business(
                    "AgentPlan.StepDoesNotRequireApproval",
                    "Step {0} does not require approval.",
                    request.StepIndex.ToString());

            if (step.ApprovedBy is not null)
                return Error.Business(
                    "AgentPlan.StepAlreadyApproved",
                    "Step {0} has already been approved by '{1}'.",
                    request.StepIndex.ToString(),
                    step.ApprovedBy);

            plan.ApproveStep(request.StepIndex, request.ApprovedBy);
            await planRepository.UpdateAsync(plan, ct);

            return new Response(
                PlanId: plan.Id.Value,
                StepIndex: request.StepIndex,
                PlanStatus: plan.PlanStatus.ToString(),
                ApprovedBy: request.ApprovedBy,
                ApprovedAt: DateTimeOffset.UtcNow);
        }
    }

    /// <summary>Resposta de aprovação de passo.</summary>
    public sealed record Response(
        Guid PlanId,
        int StepIndex,
        string PlanStatus,
        string ApprovedBy,
        DateTimeOffset ApprovedAt);
}
