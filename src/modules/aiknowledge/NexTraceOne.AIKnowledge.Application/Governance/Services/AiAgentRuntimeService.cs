using System.Diagnostics;
using System.Text.Json;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Services;

/// <summary>
/// Implementação do runtime de agents de IA.
/// Orquestra o pipeline: agent → modelo → provider → tools → inferência → artefactos.
///
/// Pipeline de execução:
/// 1. Resolve agent por ID
/// 2. Valida agent activo
/// 3. Valida acesso do utilizador ao agent
/// 4. Resolve modelo (override ou preferred do agent)
/// 5. Valida modelo permitido para o agent
/// 6. Resolve provider
/// 7. Resolve tools permitidas para o agent
/// 8. Monta prompt (system + tools + input)
/// 9. Executa inferência
/// 10. Detecta e executa tool calls (loop)
/// 11. Persiste execução
/// 12. Gera artefactos (se aplicável)
/// 13. Persiste artefactos
/// 14. Retorna resultado
/// </summary>
public sealed class AiAgentRuntimeService(
    IAiAgentRepository agentRepository,
    IAiAgentExecutionRepository executionRepository,
    IAiAgentArtifactRepository artifactRepository,
    IAiModelCatalogService modelCatalogService,
    IAiProviderFactory providerFactory,
    IToolRegistry toolRegistry,
    IToolExecutor toolExecutor,
    IToolPermissionValidator toolPermissionValidator,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider) : IAiAgentRuntimeService
{
    /// <summary>Número máximo de iterações do loop de tools para prevenir ciclos infinitos.</summary>
    private const int MaxToolIterations = 5;

    public async Task<Result<AgentExecutionResult>> ExecuteAsync(
        AiAgentId agentId,
        string input,
        Guid? modelIdOverride,
        string? contextJson,
        CancellationToken cancellationToken)
    {
        // 1. Resolve agent
        var agent = await agentRepository.GetByIdAsync(agentId, cancellationToken);
        if (agent is null)
            return AiGovernanceErrors.AgentNotFound(agentId.Value.ToString());

        // 2. Valida agent activo
        if (!agent.IsActive)
            return AiGovernanceErrors.AgentNotActive(agentId.Value.ToString());

        // 3. Valida acesso (TeamId não disponível ainda — passa null)
        if (!agent.IsAccessibleBy(currentUser.Id, teamId: null))
            return AiGovernanceErrors.AgentAccessDenied(agentId.Value.ToString());

        // 4. Resolve modelo
        var modelId = modelIdOverride ?? agent.PreferredModelId;
        ResolvedModel? resolvedModel;

        if (modelId.HasValue)
        {
            resolvedModel = await modelCatalogService.ResolveModelByIdAsync(
                modelId.Value, cancellationToken);
        }
        else
        {
            resolvedModel = await modelCatalogService.ResolveDefaultModelAsync(
                "chat", cancellationToken);
        }

        if (resolvedModel is null)
        {
            return Error.NotFound(
                "AiGovernance.Agent.NoModelAvailable",
                "No AI model available for agent execution.");
        }

        // 5. Valida modelo permitido para o agent
        if (!agent.IsModelAllowed(resolvedModel.ModelId))
        {
            return AiGovernanceErrors.ModelNotAllowedForAgent(
                resolvedModel.ModelId.ToString(), agent.DisplayName);
        }

        // 6. Resolve provider
        var chatProvider = providerFactory.GetChatProvider(resolvedModel.ProviderId);
        if (chatProvider is null)
        {
            return Error.NotFound(
                "AiGovernance.Agent.ProviderNotFound",
                "AI provider '{0}' is not available for agent execution.",
                resolvedModel.ProviderId);
        }

        // 7. Resolve tools permitidas para o agent
        var allowedTools = toolPermissionValidator.GetAllowedTools(agent.AllowedTools);

        // 8. Inicia execução
        var now = dateTimeProvider.UtcNow;
        var execution = AiAgentExecution.Start(
            agentId,
            currentUser.Id,
            resolvedModel.ModelId,
            resolvedModel.ProviderId,
            input,
            contextJson,
            now);

        await executionRepository.AddAsync(execution, cancellationToken);

        // 9. Monta prompt e executa inferência (com tool loop)
        var systemPrompt = BuildSystemPrompt(agent, allowedTools);
        var chatMessages = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            chatMessages.Add(new ChatMessage("system", systemPrompt));
        chatMessages.Add(new ChatMessage("user", input));

        var sw = Stopwatch.StartNew();
        ChatCompletionResult chatResult;
        var toolResults = new List<ToolExecutionResult>();

        try
        {
            chatResult = await chatProvider.CompleteAsync(
                new ChatCompletionRequest(
                    resolvedModel.ModelName,
                    chatMessages,
                    Temperature: 0.3,
                    MaxTokens: 4096,
                    SystemPrompt: null),
                cancellationToken);

            // 10. Tool execution loop — detect tool call patterns and execute
            if (chatResult.Success && allowedTools.Count > 0 && chatResult.Content is not null)
            {
                var iteration = 0;
                while (iteration < MaxToolIterations)
                {
                    var toolCall = DetectToolCall(chatResult.Content, allowedTools);
                    if (toolCall is null)
                        break;

                    // Validate tool is allowed
                    if (!toolPermissionValidator.IsToolAllowed(agent.AllowedTools, toolCall.ToolName))
                        break;

                    // Execute tool
                    var toolResult = await toolExecutor.ExecuteAsync(toolCall, cancellationToken);
                    toolResults.Add(toolResult);

                    // Append tool result to conversation and re-infer
                    chatMessages.Add(new ChatMessage("assistant", chatResult.Content));
                    chatMessages.Add(new ChatMessage("user",
                        $"[Tool Result for {toolCall.ToolName}]: {(toolResult.Success ? toolResult.Output : $"Error: {toolResult.ErrorMessage}")}"));

                    chatResult = await chatProvider.CompleteAsync(
                        new ChatCompletionRequest(
                            resolvedModel.ModelName,
                            chatMessages,
                            Temperature: 0.3,
                            MaxTokens: 4096,
                            SystemPrompt: null),
                        cancellationToken);

                    if (!chatResult.Success)
                        break;

                    iteration++;
                }
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            execution.Fail(ex.Message, dateTimeProvider.UtcNow, sw.ElapsedMilliseconds);
            await executionRepository.UpdateAsync(execution, cancellationToken);

            return AiGovernanceErrors.AgentExecutionFailed(
                execution.Id.Value.ToString(), ex.Message);
        }

        sw.Stop();

        if (!chatResult.Success)
        {
            execution.Fail(
                chatResult.ErrorMessage ?? "Unknown inference error",
                dateTimeProvider.UtcNow,
                sw.ElapsedMilliseconds);
            await executionRepository.UpdateAsync(execution, cancellationToken);

            return AiGovernanceErrors.AgentExecutionFailed(
                execution.Id.Value.ToString(),
                chatResult.ErrorMessage ?? "Inference failed");
        }

        // 11. Build execution steps JSON for audit trail
        var stepsJson = toolResults.Count > 0
            ? JsonSerializer.Serialize(toolResults.Select(tr => new
            {
                tool = tr.ToolName,
                success = tr.Success,
                durationMs = tr.DurationMs,
                error = tr.ErrorMessage,
            }))
            : string.Empty;

        // 12. Conclui execução
        execution.Complete(
            chatResult.Content ?? string.Empty,
            chatResult.PromptTokens,
            chatResult.CompletionTokens,
            sw.ElapsedMilliseconds,
            dateTimeProvider.UtcNow,
            stepsJson);

        await executionRepository.UpdateAsync(execution, cancellationToken);

        // 13. Incrementa contador
        agent.IncrementExecutionCount();
        await agentRepository.UpdateAsync(agent, cancellationToken);

        // 14. Gera artefactos baseado no tipo de agent
        var artifacts = await GenerateArtifactsAsync(
            agent, execution, chatResult.Content ?? string.Empty, cancellationToken);

        return new AgentExecutionResult(
            execution.Id.Value,
            agent.Id.Value,
            agent.DisplayName,
            execution.Status.ToString(),
            chatResult.Content ?? string.Empty,
            chatResult.PromptTokens,
            chatResult.CompletionTokens,
            sw.ElapsedMilliseconds,
            artifacts,
            toolResults.Select(tr => new ToolExecutionSummary(
                tr.ToolName, tr.Success, tr.DurationMs, tr.ErrorMessage)).ToList());
    }

    /// <summary>
    /// Detects a tool call pattern in the model output.
    /// Supports a simple convention: [TOOL_CALL: tool_name({"arg":"value"})]
    /// This is provider-agnostic and works with any LLM that follows instruction.
    /// </summary>
    private static ToolCallRequest? DetectToolCall(
        string content,
        IReadOnlyList<ToolDefinition> allowedTools)
    {
        const string prefix = "[TOOL_CALL:";
        const string suffix = "]";

        var startIdx = content.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (startIdx < 0)
            return null;

        var endIdx = content.IndexOf(suffix, startIdx + prefix.Length, StringComparison.Ordinal);
        if (endIdx < 0)
            return null;

        var callContent = content[(startIdx + prefix.Length)..endIdx].Trim();

        // Parse: tool_name({"arg":"value"})  or  tool_name({})  or just  tool_name
        var parenIdx = callContent.IndexOf('(');
        string toolName;
        string argsJson;

        if (parenIdx > 0)
        {
            toolName = callContent[..parenIdx].Trim();
            var argsRaw = callContent[(parenIdx + 1)..].TrimEnd(')').Trim();
            argsJson = string.IsNullOrWhiteSpace(argsRaw) ? "{}" : argsRaw;
        }
        else
        {
            toolName = callContent.Trim();
            argsJson = "{}";
        }

        // Only return if tool is in allowed list
        if (!allowedTools.Any(t => string.Equals(t.Name, toolName, StringComparison.OrdinalIgnoreCase)))
            return null;

        return new ToolCallRequest(toolName, argsJson);
    }

    private static string BuildSystemPrompt(AiAgent agent, IReadOnlyList<ToolDefinition> allowedTools)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(agent.SystemPrompt))
            parts.Add(agent.SystemPrompt);

        if (!string.IsNullOrWhiteSpace(agent.Objective))
            parts.Add($"Objective: {agent.Objective}");

        if (!string.IsNullOrWhiteSpace(agent.OutputSchema))
            parts.Add($"Expected output format: {agent.OutputSchema}");

        // Inject tool descriptions into system prompt when tools are available
        if (allowedTools.Count > 0)
        {
            var toolsSection = new List<string>
            {
                "\n## Available Tools",
                "You have access to the following tools. To call a tool, use this exact format:",
                "[TOOL_CALL: tool_name({\"param\": \"value\"})]",
                "\nAvailable tools:"
            };

            foreach (var tool in allowedTools)
            {
                var paramDesc = tool.Parameters.Count > 0
                    ? string.Join(", ", tool.Parameters.Select(p =>
                        $"{p.Name} ({p.Type}{(p.Required ? ", required" : "")})"))
                    : "none";

                toolsSection.Add($"- **{tool.Name}**: {tool.Description}");
                toolsSection.Add($"  Parameters: {paramDesc}");
            }

            toolsSection.Add("\nOnly call tools when you need data to complete the task. Include the tool call in your response.");

            parts.Add(string.Join("\n", toolsSection));
        }

        return string.Join("\n\n", parts);
    }

    private async Task<IReadOnlyList<AgentArtifactResult>> GenerateArtifactsAsync(
        AiAgent agent,
        AiAgentExecution execution,
        string output,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(output))
            return [];

        var artifactType = DeriveArtifactType(agent.Category);
        if (artifactType is null)
            return [];

        var format = DeriveArtifactFormat(artifactType.Value);
        var title = $"{agent.DisplayName} — Output v{agent.Version}";

        var artifact = AiAgentArtifact.Create(
            execution.Id,
            agent.Id,
            artifactType.Value,
            title,
            output,
            format);

        await artifactRepository.AddAsync(artifact, cancellationToken);

        return
        [
            new AgentArtifactResult(
                artifact.Id.Value,
                artifactType.Value.ToString(),
                title,
                format)
        ];
    }

    private static AgentArtifactType? DeriveArtifactType(AgentCategory category) => category switch
    {
        AgentCategory.ContractGovernance => AgentArtifactType.OpenApiDraft,
        AgentCategory.ApiDesign => AgentArtifactType.OpenApiDraft,
        AgentCategory.SoapDesign => AgentArtifactType.SoapContractDraft,
        AgentCategory.TestGeneration => AgentArtifactType.TestScenarios,
        AgentCategory.EventDesign => AgentArtifactType.KafkaSchema,
        AgentCategory.DocumentationAssistance => AgentArtifactType.Documentation,
        AgentCategory.SecurityAudit => AgentArtifactType.Analysis,
        AgentCategory.CodeReview => AgentArtifactType.CodeReview,
        _ => null,
    };

    private static string DeriveArtifactFormat(AgentArtifactType type) => type switch
    {
        AgentArtifactType.OpenApiDraft => "yaml",
        AgentArtifactType.SoapContractDraft => "xml",
        AgentArtifactType.TestScenarios => "json",
        AgentArtifactType.KafkaSchema => "json",
        AgentArtifactType.Documentation => "markdown",
        AgentArtifactType.Analysis => "markdown",
        AgentArtifactType.CodeReview => "markdown",
        AgentArtifactType.Checklist => "markdown",
        _ => "text",
    };
}
