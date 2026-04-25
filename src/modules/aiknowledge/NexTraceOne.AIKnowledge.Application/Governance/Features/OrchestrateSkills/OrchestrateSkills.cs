using System.Text;
using System.Text.Json.Nodes;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.OrchestrateSkills;

/// <summary>
/// Feature: OrchestrateSkills — agente de alto nível que decompõe uma task em skills via planning.
/// O orchestrator usa o LLM para selecionar e ordenar skills da catalog disponível,
/// depois executa o pipeline resultante.
/// Estrutura VSA: Command + Validator + Handler + Response.
/// </summary>
public static class OrchestrateSkills
{
    public const int MaxOrchestratedSkills = 5;

    /// <summary>Comando de orquestração de skills para uma task.</summary>
    public sealed record Command(
        string TaskDescription,
        string InputJson,
        string ExecutedBy,
        Guid TenantId,
        string? ModelOverride = null) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TaskDescription).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.InputJson).NotEmpty();
            RuleFor(x => x.ExecutedBy).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler que usa o LLM para construir um plan de skills e depois os executa em cadeia.
    /// </summary>
    public sealed class Handler(
        ISkillRegistry skillRegistry,
        ISkillLoader skillLoader,
        IAiSkillRepository skillRepository,
        ISkillExecutor skillExecutor,
        IAiSkillExecutionRepository executionRepository,
        IAiProviderFactory providerFactory,
        IAiModelCatalogService modelCatalog,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Step 1: get available skills
            var availableSkills = await skillLoader.ListActiveSummariesAsync(request.TenantId, cancellationToken);
            if (availableSkills.Count == 0)
                return Error.Business(
                    "AiGovernance.Skill.NoSkillsAvailable",
                    "No active skills available for orchestration.");

            // Step 2: ask LLM to select which skills to run and in what order
            var skillPlan = await PlanSkillsAsync(
                request.TaskDescription, availableSkills, request.ModelOverride, cancellationToken);

            if (skillPlan.Count == 0)
                return Error.Business(
                    "AiGovernance.Skill.OrchestratorNoPlan",
                    "Orchestrator could not determine a skill plan for this task.");

            // Step 3: execute each skill in the plan
            var stepResults = new List<StepResult>();
            var currentInput = request.InputJson;

            for (var i = 0; i < skillPlan.Count; i++)
            {
                var skillName = skillPlan[i];
                var skill = await skillRepository.GetByNameAsync(skillName, request.TenantId, cancellationToken)
                         ?? await skillRepository.GetByNameAsync(skillName, Guid.Empty, cancellationToken);

                if (skill is null)
                    continue;

                var output = await skillExecutor.ExecuteAsync(
                    skill, currentInput, request.ModelOverride,
                    request.TenantId, request.ExecutedBy, cancellationToken);

                var log = AiSkillExecution.Log(
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
                executionRepository.Add(log);
                skill.IncrementExecutionCount();

                stepResults.Add(new StepResult(
                    Step: i + 1,
                    SkillName: skillName,
                    Success: output.Success,
                    OutputJson: output.OutputJson,
                    ErrorMessage: output.ErrorMessage));

                if (!output.Success)
                    break;

                currentInput = output.OutputJson;
            }

            return new Response(
                TaskDescription: request.TaskDescription,
                SkillPlan: skillPlan,
                Steps: stepResults,
                FinalOutputJson: currentInput,
                CompletedSteps: stepResults.Count(s => s.Success));
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private async Task<IReadOnlyList<string>> PlanSkillsAsync(
            string task,
            IReadOnlyList<SkillSummaryDto> availableSkills,
            string? modelOverride,
            CancellationToken ct)
        {
            var resolvedModel = modelOverride is not null
                ? await modelCatalog.ResolveDefaultModelAsync("chat", ct) is { } m
                    ? m with { ModelName = modelOverride }
                    : null
                : await modelCatalog.ResolveDefaultModelAsync("chat", ct);

            if (resolvedModel is null) return [];

            var provider = providerFactory.GetChatProvider(resolvedModel.ProviderId);
            if (provider is null) return [];

            var catalogSummary = BuildCatalogSummary(availableSkills);
            var systemPrompt =
                "You are a skill orchestrator. Given a task and a catalog of available skills, " +
                "select the minimal set of skills needed (max 5) and return them as a JSON array of skill names. " +
                "Return ONLY the JSON array, no explanation. Example: [\"incident-triage\",\"runbook-generator\"]";

            var userMessage = $"Task: {task}\n\nAvailable skills:\n{catalogSummary}";

            var result = await provider.CompleteAsync(
                new ChatCompletionRequest(
                    resolvedModel.ModelName,
                    [new ChatMessage("user", userMessage)],
                    Temperature: 0.1,
                    MaxTokens: 512,
                    SystemPrompt: systemPrompt),
                ct);

            if (!result.Success || string.IsNullOrWhiteSpace(result.Content))
                return [];

            return ParseSkillPlan(result.Content, availableSkills);
        }

        private static string BuildCatalogSummary(IReadOnlyList<SkillSummaryDto> skills)
        {
            var sb = new StringBuilder();
            foreach (var s in skills)
                sb.AppendLine($"- {s.Name}: {s.Description}");
            return sb.ToString().TrimEnd();
        }

        private static IReadOnlyList<string> ParseSkillPlan(
            string content,
            IReadOnlyList<SkillSummaryDto> availableSkills)
        {
            var trimmed = content.Trim();
            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                var first = trimmed.IndexOf('\n');
                var last = trimmed.LastIndexOf("```", StringComparison.Ordinal);
                if (first >= 0 && last > first)
                    trimmed = trimmed[(first + 1)..last].Trim();
            }

            try
            {
                var array = JsonNode.Parse(trimmed)?.AsArray();
                if (array is null) return [];

                var validNames = availableSkills.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                return array
                    .Select(n => n?.GetValue<string>() ?? string.Empty)
                    .Where(n => validNames.Contains(n))
                    .Take(MaxOrchestratedSkills)
                    .ToList()
                    .AsReadOnly();
            }
            catch
            {
                return [];
            }
        }
    }

    public sealed record StepResult(
        int Step,
        string SkillName,
        bool Success,
        string OutputJson,
        string? ErrorMessage = null);

    public sealed record Response(
        string TaskDescription,
        IReadOnlyList<string> SkillPlan,
        IReadOnlyList<StepResult> Steps,
        string FinalOutputJson,
        int CompletedSteps);
}
