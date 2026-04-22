using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.SubmitAgentExecutionPlan;

/// <summary>
/// Feature: SubmitAgentExecutionPlan — submete um plano de execução agentic multi-passo.
/// Valida o número de passos contra o limite configurado e cria o plano em estado Pending.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class SubmitAgentExecutionPlan
{
    /// <summary>Representa um passo a configurar no plano.</summary>
    public sealed record StepRequest(
        int StepIndex,
        string Name,
        string StepType,
        string InputJson,
        bool RequiresApproval);

    /// <summary>Comando de submissão de um plano de execução agentic.</summary>
    public sealed record Command(
        Guid TenantId,
        string RequestedBy,
        string Description,
        IReadOnlyList<StepRequest> Steps,
        int MaxTokenBudget,
        bool RequiresApproval,
        int BlastRadiusThreshold,
        string? CorrelationId) : ICommand<Response>;

    /// <summary>Validador do comando de submissão de plano.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
            RuleFor(x => x.Steps).NotEmpty();
            RuleFor(x => x.MaxTokenBudget).GreaterThan(0);
            RuleFor(x => x.BlastRadiusThreshold).GreaterThanOrEqualTo(0);
            RuleForEach(x => x.Steps).ChildRules(step =>
            {
                step.RuleFor(s => s.Name).NotEmpty().MaximumLength(200);
                step.RuleFor(s => s.StepType).NotEmpty();
            });
        }
    }

    /// <summary>Handler que valida e persiste o plano de execução.</summary>
    public sealed class Handler(
        IAgentExecutionPlanRepository planRepository,
        IConfigurationResolutionService configService) : ICommandHandler<Command, Response>
    {
        private const string MaxStepsKey = "ai.agentic.max_steps_per_plan";
        private const int DefaultMaxSteps = 10;

        public async Task<Result<Response>> Handle(Command request, CancellationToken ct)
        {
            Guard.Against.Null(request);

            var maxStepsConfig = await configService.ResolveEffectiveValueAsync(
                MaxStepsKey, ConfigurationScope.System, null, ct);
            var maxSteps = int.TryParse(maxStepsConfig?.EffectiveValue, out var parsed)
                ? parsed : DefaultMaxSteps;

            if (request.Steps.Count > maxSteps)
                return Error.Business(
                    "AgentPlan.TooManySteps",
                    "Plan has {0} steps but the configured maximum is {1}.",
                    request.Steps.Count.ToString(),
                    maxSteps.ToString());

            var agentSteps = request.Steps
                .Select(s => new AgentStep
                {
                    StepIndex = s.StepIndex,
                    Name = s.Name,
                    StepType = Enum.TryParse<AgentStepType>(s.StepType, true, out var t)
                        ? t : AgentStepType.ContractLookup,
                    InputJson = s.InputJson,
                    RequiresApproval = s.RequiresApproval,
                    Status = AgentExecutionStatus.Pending,
                })
                .ToList();

            var plan = AgentExecutionPlan.Submit(
                tenantId: request.TenantId,
                requestedBy: request.RequestedBy,
                description: request.Description,
                steps: agentSteps,
                maxTokenBudget: request.MaxTokenBudget,
                requiresApproval: request.RequiresApproval,
                blastRadiusThreshold: request.BlastRadiusThreshold,
                correlationId: request.CorrelationId);

            await planRepository.AddAsync(plan, ct);

            return new Response(
                PlanId: plan.Id.Value,
                CorrelationId: plan.CorrelationId,
                Status: plan.PlanStatus.ToString(),
                StepCount: plan.Steps.Count,
                MaxTokenBudget: plan.MaxTokenBudget);
        }
    }

    /// <summary>Resposta de submissão de plano de execução.</summary>
    public sealed record Response(
        Guid PlanId,
        string CorrelationId,
        string Status,
        int StepCount,
        int MaxTokenBudget);
}
