using System.Diagnostics;
using System.Text;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Services;

/// <summary>
/// Executa AiSkills através do AI Runtime.
/// Constrói o prompt a partir do SkillContent, injeta o inputJson como mensagem do utilizador
/// e invoca o provider de chat via IAiProviderFactory + IAiModelCatalogService.
/// </summary>
internal sealed class SkillExecutorService : ISkillExecutor
{
    private readonly IAiProviderFactory _providerFactory;
    private readonly IAiModelCatalogService _modelCatalog;
    private readonly ILogger<SkillExecutorService> _logger;

    public SkillExecutorService(
        IAiProviderFactory providerFactory,
        IAiModelCatalogService modelCatalog,
        ILogger<SkillExecutorService> logger)
    {
        _providerFactory = providerFactory;
        _modelCatalog = modelCatalog;
        _logger = logger;
    }

    public async Task<SkillExecutionOutput> ExecuteAsync(
        AiSkill skill,
        string inputJson,
        string? modelOverride,
        Guid tenantId,
        string executedBy,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        // Resolve provider and model
        var resolvedModel = await ResolveModelAsync(skill, modelOverride, ct);
        if (resolvedModel is null)
        {
            sw.Stop();
            return new SkillExecutionOutput(
                false, "{}", "none", "none",
                0, 0, sw.Elapsed,
                "No AI model/provider could be resolved for skill execution");
        }

        var provider = _providerFactory.GetChatProvider(resolvedModel.ProviderId);
        if (provider is null)
        {
            sw.Stop();
            return new SkillExecutionOutput(
                false, "{}", resolvedModel.ModelName, resolvedModel.ProviderId,
                0, 0, sw.Elapsed,
                $"Provider '{resolvedModel.ProviderId}' is not available");
        }

        var systemPrompt = BuildSystemPrompt(skill);
        var userMessage = BuildUserMessage(skill, inputJson);

        var request = new ChatCompletionRequest(
            resolvedModel.ModelName,
            [new ChatMessage("user", userMessage)],
            Temperature: 0.2,
            MaxTokens: 4096,
            SystemPrompt: systemPrompt);

        try
        {
            var result = await provider.CompleteAsync(request, ct);
            sw.Stop();

            if (!result.Success)
            {
                return new SkillExecutionOutput(
                    false, "{}", result.ModelId, result.ProviderId,
                    result.PromptTokens, result.CompletionTokens, sw.Elapsed,
                    result.ErrorMessage);
            }

            var outputJson = WrapAsJson(result.Content ?? string.Empty, skill.Name);

            return new SkillExecutionOutput(
                true, outputJson,
                result.ModelId, result.ProviderId,
                result.PromptTokens, result.CompletionTokens, sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Skill {SkillName} execution failed for model {Model}",
                skill.Name, resolvedModel.ModelName);
            return new SkillExecutionOutput(
                false, "{}", resolvedModel.ModelName, resolvedModel.ProviderId,
                0, 0, sw.Elapsed, ex.Message);
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<ResolvedModel?> ResolveModelAsync(
        AiSkill skill, string? modelOverride, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(modelOverride))
        {
            // Try by name via preferred model resolution (fallback to default chat)
            var defaultModel = await _modelCatalog.ResolveDefaultModelAsync("chat", ct);
            if (defaultModel is not null)
                return defaultModel with { ModelName = modelOverride };
        }

        // Use first preferred model of the skill if set
        if (skill.PreferredModels.Length > 0)
        {
            var preferredModelName = skill.PreferredModels
                .Split(',', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            var defaultModel = await _modelCatalog.ResolveDefaultModelAsync("chat", ct);
            if (defaultModel is not null)
                return defaultModel with { ModelName = preferredModelName };
        }

        return await _modelCatalog.ResolveDefaultModelAsync("chat", ct);
    }

    private static string BuildSystemPrompt(AiSkill skill)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"You are executing the '{skill.DisplayName}' skill.");
        if (!string.IsNullOrWhiteSpace(skill.Description))
            sb.AppendLine(skill.Description);
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(skill.SkillContent))
        {
            sb.AppendLine("## Skill Instructions");
            sb.AppendLine(skill.SkillContent);
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(skill.OutputSchema))
        {
            sb.AppendLine("## Expected Output Format");
            sb.AppendLine(skill.OutputSchema);
        }

        return sb.ToString().TrimEnd();
    }

    private static string BuildUserMessage(AiSkill skill, string inputJson)
    {
        if (!string.IsNullOrWhiteSpace(skill.InputSchema))
            return $"Execute the skill with the following input:\n\n```json\n{inputJson}\n```";
        return inputJson;
    }

    private static string WrapAsJson(string content, string skillName)
    {
        var trimmed = content.Trim();
        if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
            return trimmed;

        var escaped = System.Text.Json.JsonSerializer.Serialize(content);
        return $"{{\"skill\":\"{skillName}\",\"output\":{escaped}}}";
    }
}
