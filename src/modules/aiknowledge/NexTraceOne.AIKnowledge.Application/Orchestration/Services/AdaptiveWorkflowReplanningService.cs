using System.Text.Json;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Orchestration.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.SemanticKernel;
using NexTraceOne.AIKnowledge.Application.Runtime.Utils;

namespace NexTraceOne.AIKnowledge.Application.Orchestration.Services;

/// <summary>
/// Implementação de replaneamento adaptativo usando LLM.
/// Gera um novo plano de workflow quando um step falha, tentando substituir
/// o agente problemático ou ajustar os inputs dos steps restantes.
/// </summary>
public sealed class AdaptiveWorkflowReplanningService(
    IAiKernelService kernelService,
    IAiProviderFactory providerFactory,
    ILogger<AdaptiveWorkflowReplanningService> logger) : IWorkflowReplanningService
{
    public async Task<AgentWorkflowDefinition?> ReplanAsync(
        AgentWorkflowDefinition originalWorkflow,
        IReadOnlyList<AgentWorkflowStepResult> completedSteps,
        AgentWorkflowStep failedStep,
        string errorMessage,
        string currentOutput,
        CancellationToken cancellationToken = default)
    {
        var provider = providerFactory.GetChatProvider("ollama")
            ?? providerFactory.GetChatProvider("openai");

        if (provider is null)
        {
            logger.LogWarning("No AI provider available for workflow replanning");
            return null;
        }

        try
        {
            var kernel = kernelService.CreateKernel(provider.ProviderId, provider.ProviderId);

            var systemPrompt = """
                You are a workflow replanning expert. Given a workflow that failed at a specific step,
                generate a revised plan for the remaining steps.
                
                Respond ONLY with valid JSON. No markdown, no explanations.
                
                Expected JSON format:
                {
                  "steps": [
                    {
                      "agentId": "guid-string",
                      "inputTemplate": "template with {previousOutput} placeholder",
                      "parallelGroupId": null
                    }
                  ]
                }
                
                Rules:
                - Use only the agent IDs from the original workflow (or omit steps if no alternative exists).
                - Adjust input templates to work around the failure.
                - parallelGroupId is optional (null for sequential, same integer for parallel).
                - If the failure is unrecoverable, return {"steps": []}.
                """;

            var completedSummary = string.Join("\n", completedSteps.Select(s =>
                $"- Step {s.AgentName}: {(s.Success ? "SUCCESS" : "FAILED")} — {s.Output[..Math.Min(100, s.Output.Length)]}"));

            var originalStepsJson = JsonSerializer.Serialize(originalWorkflow.Steps.Select(s => new
            {
                agentId = s.AgentId,
                inputTemplate = s.InputTemplate,
                parallelGroupId = s.ParallelGroupId
            }));

            var userPrompt = $"""
                Original workflow: {originalWorkflow.Name}
                
                Original steps:
                {originalStepsJson}
                
                Completed steps:
                {completedSummary}
                
                Failed step:
                - AgentId: {failedStep.AgentId}
                - InputTemplate: {failedStep.InputTemplate}
                - Error: {errorMessage}
                
                Current accumulated output:
                {currentOutput}
                
                Please generate a revised plan for the remaining steps.
                """;

            var messages = new List<ChatMessage> { new("user", userPrompt) };
            var response = await kernelService.ExecuteChatAsync(
                kernel, systemPrompt, messages, cancellationToken);

            if (!LlmJsonParser.TryParse<ReplanLlmOutput>(response, out var replan) || replan is null)
            {
                logger.LogWarning("Failed to parse replanning JSON. Raw: {Raw}", response[..Math.Min(200, response.Length)]);
                return null;
            }

            if (replan.Steps is null || replan.Steps.Count == 0)
            {
                logger.LogInformation("Replanning determined the workflow is unrecoverable");
                return null;
            }

            var newSteps = replan.Steps
                .Where(s => !string.IsNullOrWhiteSpace(s.AgentId))
                .Select(s => new AgentWorkflowStep(
                    Guid.Parse(s.AgentId!),
                    s.InputTemplate,
                    s.ParallelGroupId))
                .ToList();

            logger.LogInformation(
                "Workflow replanned: {OriginalName} now has {StepCount} remaining steps",
                originalWorkflow.Name, newSteps.Count);

            return new AgentWorkflowDefinition($"{originalWorkflow.Name} (replanned)", newSteps);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Replanning failed for workflow {Workflow}", originalWorkflow.Name);
            return null;
        }
    }

    private sealed class ReplanLlmOutput
    {
        public List<ReplanStepOutput>? Steps { get; set; }
    }

    private sealed class ReplanStepOutput
    {
        public string? AgentId { get; set; }
        public string? InputTemplate { get; set; }
        public int? ParallelGroupId { get; set; }
    }
}
