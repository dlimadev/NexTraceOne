using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ExecuteSkillPipeline;

/// <summary>
/// Feature: ExecuteSkillPipeline — executa uma sequência de skills em cadeia.
/// O output de cada step é passado como input do próximo step.
/// Máximo de 5 steps por pipeline (configurável via MaxSteps).
/// Estrutura VSA: Command + Validator + Handler + Response.
/// </summary>
public static class ExecuteSkillPipeline
{
    public const int MaxSteps = 5;

    /// <summary>Definição de um step do pipeline.</summary>
    public sealed record PipelineStep(
        Guid SkillId,
        string? ModelOverride = null);

    /// <summary>Comando de execução do pipeline.</summary>
    public sealed record Command(
        IReadOnlyList<PipelineStep> Steps,
        string InitialInputJson,
        string ExecutedBy,
        Guid TenantId) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Steps)
                .NotEmpty()
                .Must(s => s.Count <= MaxSteps)
                    .WithMessage($"Pipeline cannot exceed {MaxSteps} steps.");
            RuleForEach(x => x.Steps).ChildRules(step =>
                step.RuleFor(s => s.SkillId).NotEmpty());
            RuleFor(x => x.InitialInputJson).NotEmpty();
            RuleFor(x => x.ExecutedBy).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que executa cada skill em sequência, encadeando outputs como inputs.</summary>
    public sealed class Handler(
        IAiSkillRepository skillRepository,
        IAiSkillExecutionRepository executionRepository,
        ISkillExecutor skillExecutor,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var stepResults = new List<StepResult>();
            var currentInput = request.InitialInputJson;

            for (var i = 0; i < request.Steps.Count; i++)
            {
                var step = request.Steps[i];

                var skill = await skillRepository.GetByIdAsync(
                    AiSkillId.From(step.SkillId), cancellationToken);

                if (skill is null)
                    return AiGovernanceErrors.SkillNotFound(step.SkillId.ToString());

                if (skill.Status != SkillStatus.Active)
                    return AiGovernanceErrors.SkillNotActive(skill.Name);

                var output = await skillExecutor.ExecuteAsync(
                    skill, currentInput, step.ModelOverride,
                    request.TenantId, request.ExecutedBy, cancellationToken);

                // Log each step execution
                var executionLog = AiSkillExecution.Log(
                    skillId: skill.Id,
                    executedBy: request.ExecutedBy,
                    modelUsed: output.ModelUsed,
                    inputJson: currentInput,
                    outputJson: output.OutputJson,
                    durationMs: (long)output.Duration.TotalMilliseconds,
                    promptTokens: output.PromptTokens,
                    completionTokens: output.CompletionTokens,
                    isSuccess: output.Success,
                    errorMessage: output.ErrorMessage,
                    tenantId: request.TenantId,
                    executedAt: dateTimeProvider.UtcNow,
                    agentId: null);
                executionRepository.Add(executionLog);
                skill.IncrementExecutionCount();

                stepResults.Add(new StepResult(
                    Step: i + 1,
                    SkillId: skill.Id.Value,
                    SkillName: skill.Name,
                    Success: output.Success,
                    OutputJson: output.OutputJson,
                    ModelUsed: output.ModelUsed,
                    ErrorMessage: output.ErrorMessage));

                if (!output.Success)
                    return Error.Business(
                        "AiGovernance.Skill.PipelineStepFailed",
                        $"Pipeline failed at step {i + 1} ({skill.Name}): {output.ErrorMessage}");

                // Chain output as next step input
                currentInput = output.OutputJson;
            }

            var totalPromptTokens = stepResults.Count;
            return new Response(
                Steps: stepResults,
                FinalOutputJson: currentInput,
                TotalSteps: request.Steps.Count,
                CompletedSteps: stepResults.Count(s => s.Success));
        }
    }

    /// <summary>Resultado de um step individual no pipeline.</summary>
    public sealed record StepResult(
        int Step,
        Guid SkillId,
        string SkillName,
        bool Success,
        string OutputJson,
        string ModelUsed,
        string? ErrorMessage = null);

    /// <summary>Resposta completa do pipeline.</summary>
    public sealed record Response(
        IReadOnlyList<StepResult> Steps,
        string FinalOutputJson,
        int TotalSteps,
        int CompletedSteps);
}
