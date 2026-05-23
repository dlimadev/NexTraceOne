using FluentValidation;
using NexTraceOne.AIKnowledge.Application.Orchestration.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Orchestration.Features.ExecuteAgentWorkflow;

/// <summary>
/// Executa um workflow multi-agent em sequência.
/// Cada agente recebe o output do anterior como input.
/// </summary>
public static class ExecuteAgentWorkflow
{
    public sealed record Command(
        string WorkflowName,
        IReadOnlyList<Guid> AgentIds,
        string InitialInput,
        string? CallerTeamId = null) : ICommand<Response>;

    public sealed record Response(
        bool Success,
        IReadOnlyList<AgentWorkflowStepResultDto> Steps,
        string? FinalOutput = null,
        string? ErrorMessage = null);

    public sealed record AgentWorkflowStepResultDto(
        Guid AgentId,
        string AgentName,
        string Input,
        string Output,
        long DurationMs,
        bool Success,
        string? ErrorMessage = null);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.WorkflowName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.AgentIds).NotEmpty().Must(x => x.Count <= 10)
                .WithMessage("Workflow cannot exceed 10 agents");
            RuleFor(x => x.InitialInput).NotEmpty().MaximumLength(50_000);
        }
    }

    internal sealed class Handler(
        IAgentWorkflowOrchestrator orchestrator) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var steps = request.AgentIds.Select(id => new AgentWorkflowStep(id)).ToList();
            var workflow = new AgentWorkflowDefinition(request.WorkflowName, steps);

            var result = await orchestrator.ExecuteSequentialAsync(
                workflow,
                request.InitialInput,
                request.CallerTeamId,
                enableAdaptiveReplanning: false,
                cancellationToken);

            if (result.IsFailure)
            {
                return result.Error;
            }

            var stepDtos = result.Value.StepResults.Select(s => new AgentWorkflowStepResultDto(
                s.AgentId, s.AgentName, s.Input, s.Output, s.DurationMs, s.Success, s.ErrorMessage)).ToList();

            return new Response(
                result.Value.Success,
                stepDtos,
                result.Value.FinalOutput);
        }
    }
}
