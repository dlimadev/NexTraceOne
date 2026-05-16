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
/// 5a. Pré-valida quota de tokens antes de inferir (E-C01)
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
    IAiExecutionPlanRepository executionPlanRepository,
    IAiModelCatalogService modelCatalogService,
    IAiProviderFactory providerFactory,
    IToolRegistry toolRegistry,
    IToolExecutor toolExecutor,
    IToolPermissionValidator toolPermissionValidator,
    IAiTokenQuotaService tokenQuotaService,
    IContextWindowManager contextWindow,
    ICurrentUser currentUser,
    ICurrentTenant currentTenant,
    IDateTimeProvider dateTimeProvider) : IAiAgentRuntimeService
{
    /// <summary>Número máximo de iterações do loop de tools para prevenir ciclos infinitos.</summary>
    private const int MaxToolIterations = 5;

    /// <summary>Context window padrão em tokens quando o modelo não especifica.</summary>
    private const int DefaultContextWindowTokens = 4096;

    /// <summary>Tokens reservados para a resposta do modelo.</summary>
    private const int ReservedCompletionTokens = 1024;

    /// <summary>Estimativa conservadora de tokens por carácter para pré-validação de quota.</summary>
    private const double TokensPerCharEstimate = 0.25;

    public async Task<Result<AgentExecutionResult>> ExecuteAsync(
        AiAgentId agentId,
        string input,
        Guid? modelIdOverride,
        string? contextJson,
        string? callerTeamId,
        CancellationToken cancellationToken)
    {
        // 1. Resolve agent
        var agent = await agentRepository.GetByIdAsync(agentId, cancellationToken);
        if (agent is null)
            return AiGovernanceErrors.AgentNotFound(agentId.Value.ToString());

        // 2. Valida agent activo
        if (!agent.IsActive)
            return AiGovernanceErrors.AgentNotActive(agentId.Value.ToString());

        // 3. Valida acesso — passa o teamId real do caller para enforçar visibilidade por equipa
        if (!agent.IsAccessibleBy(currentUser.Id, teamId: callerTeamId))
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

        // 5a. Pré-valida quota de tokens antes de consumir recursos de inferência.
        // Estimativa conservadora: caracteres / 4 + tokens reservados para completion.
        var estimatedTokens = (int)(input.Length * TokensPerCharEstimate) + ReservedCompletionTokens;
        var quotaResult = await tokenQuotaService.ValidateQuotaAsync(
            currentUser.Id,
            currentTenant.Id,
            resolvedModel.ProviderId,
            resolvedModel.ModelId.ToString(),
            estimatedTokens,
            cancellationToken);

        if (!quotaResult.IsAllowed)
        {
            return AiGovernanceErrors.QuotaExceeded(
                quotaResult.PolicyName ?? "agent",
                quotaResult.BlockReason ?? currentUser.Id);
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

        // 7a. Planning mode — gera plano de execução quando activado
        string? planSummary = null;
        AIExecutionPlan? executionPlan = null;
        if (agent.UsePlanningMode)
        {
            planSummary = await TryBuildExecutionPlanAsync(agent, input, chatProvider, resolvedModel, cancellationToken);
        }

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

        // E-A04: criar e persistir plano de execução quando em planning mode, ligando-o à execução.
        if (!string.IsNullOrWhiteSpace(planSummary))
        {
            executionPlan = AIExecutionPlan.Create(
                correlationId: Guid.NewGuid().ToString(),
                inputQuery: input,
                persona: currentUser.Id,
                useCaseType: AIUseCaseType.General,
                selectedModel: resolvedModel.ModelName,
                selectedProvider: resolvedModel.ProviderId,
                isInternal: resolvedModel.IsInternal,
                routingPath: AIRoutingPath.InternalOnly,
                selectedSources: string.Empty,
                sourceWeightingSummary: string.Empty,
                policyDecision: string.Empty,
                estimatedCostClass: "low",
                rationaleSummary: planSummary,
                confidenceLevel: AIConfidenceLevel.Medium,
                escalationReason: AIEscalationReason.None,
                plannedAt: now,
                executionId: execution.Id);
            executionPlan.LinkToExecution(execution.Id);
            await executionPlanRepository.AddAsync(executionPlan, cancellationToken);
        }

        // 9. Monta prompt com contexto e executa inferência (com tool loop)
        var systemPrompt = BuildSystemPrompt(agent, allowedTools, contextJson);
        if (!string.IsNullOrWhiteSpace(planSummary))
            systemPrompt = $"## Execution Plan\n{planSummary}\n\n{systemPrompt}".TrimStart();
        var chatMessages = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            chatMessages.Add(new ChatMessage("system", systemPrompt));
        chatMessages.Add(new ChatMessage("user", input));

        var sw = Stopwatch.StartNew();
        ChatCompletionResult chatResult;
        var toolResults = new List<ToolExecutionResult>();

        // Determine context window limit from resolved model
        var contextWindowTokens = resolvedModel.ContextWindow ?? DefaultContextWindowTokens;

        try
        {
            // Apply context window trimming before first inference
            var (trimmedMessages, wasTruncated) = contextWindow.TrimToFit(
                chatMessages, contextWindowTokens, ReservedCompletionTokens);
            if (wasTruncated)
            {
                chatMessages = trimmedMessages.ToList();
            }

            // 10. Inference + tool execution loop
            // When the provider supports native function calling (OpenAI/Anthropic), use structured
            // tool calls for 100% reliability. Otherwise fall back to the textual [TOOL_CALL:] pattern
            // which is compatible with Ollama and other local providers.
            if (chatProvider is IFunctionCallingChatProvider nativeProvider && allowedTools.Count > 0)
            {
                var functions = allowedTools.Select(BuildFunctionDefinition).ToList();
                var iteration = 0;
                FunctionCallingResult fcResult;

                do
                {
                    fcResult = await nativeProvider.CompleteWithToolsAsync(
                        new ChatCompletionRequest(
                            resolvedModel.ModelName,
                            chatMessages,
                            Temperature: 0.3,
                            MaxTokens: 4096,
                            SystemPrompt: null),
                        functions,
                        cancellationToken);

                    chatResult = FunctionCallingResultToChatResult(fcResult);

                    if (!fcResult.Success || !fcResult.HasToolCalls)
                        break;

                    // Execute all tool calls returned by the model in this round
                    foreach (var nativeCall in fcResult.ToolCalls)
                    {
                        var toolResult = await toolExecutor.ExecuteAsync(
                            new ToolCallRequest(nativeCall.FunctionName, nativeCall.ArgumentsJson),
                            cancellationToken);
                        toolResults.Add(toolResult);

                        // Append tool result as user message (compatible with all providers)
                        chatMessages.Add(new ChatMessage("assistant",
                            fcResult.Content ?? $"[TOOL_CALL_ID: {nativeCall.Id}]"));
                        chatMessages.Add(new ChatMessage("user",
                            $"[Tool Result for {nativeCall.FunctionName}]: {(toolResult.Success ? toolResult.Output : $"Error: {toolResult.ErrorMessage}")}"));
                    }

                    var (trimmedNative, nativeTruncated) = contextWindow.TrimToFit(
                        chatMessages, contextWindowTokens, ReservedCompletionTokens);
                    if (nativeTruncated)
                        chatMessages = trimmedNative.ToList();

                    iteration++;
                }
                while (iteration < MaxToolIterations);
            }
            else
            {
                // Textual tool detection path — compatible with Ollama and providers without
                // native function calling support.
                chatResult = await chatProvider.CompleteAsync(
                    new ChatCompletionRequest(
                        resolvedModel.ModelName,
                        chatMessages,
                        Temperature: 0.3,
                        MaxTokens: 4096,
                        SystemPrompt: null),
                    cancellationToken);

                if (chatResult.Success && allowedTools.Count > 0 && chatResult.Content is not null)
                {
                    var iteration = 0;
                    while (iteration < MaxToolIterations)
                    {
                        var toolCall = DetectToolCall(chatResult.Content!, allowedTools);
                        if (toolCall is null)
                            break;

                        var toolResult = await toolExecutor.ExecuteAsync(toolCall, cancellationToken);
                        toolResults.Add(toolResult);

                        chatMessages.Add(new ChatMessage("assistant", chatResult.Content!));
                        chatMessages.Add(new ChatMessage("user",
                            $"[Tool Result for {toolCall.ToolName}]: {(toolResult.Success ? toolResult.Output : $"Error: {toolResult.ErrorMessage}")}"));

                        var (trimmedLoop, _) = contextWindow.TrimToFit(
                            chatMessages, contextWindowTokens, ReservedCompletionTokens);

                        chatResult = await chatProvider.CompleteAsync(
                            new ChatCompletionRequest(
                                resolvedModel.ModelName,
                                trimmedLoop,
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
                tr.ToolName, tr.Success, tr.DurationMs, tr.ErrorMessage)).ToList(),
            PlanSummary: planSummary,
            PlanId: executionPlan?.Id.Value);
    }

    /// <summary>
    /// Converte um FunctionCallingResult num ChatCompletionResult para compatibilidade
    /// com o restante pipeline do agent runtime.
    /// </summary>
    private static ChatCompletionResult FunctionCallingResultToChatResult(FunctionCallingResult fcResult)
        => new(
            fcResult.Success,
            fcResult.Content,
            fcResult.ModelId,
            fcResult.ProviderId,
            fcResult.PromptTokens,
            fcResult.CompletionTokens,
            fcResult.Duration,
            fcResult.ErrorMessage);

    /// <summary>
    /// Constrói uma FunctionDefinition a partir de uma ToolDefinition para uso em native function calling.
    /// O JSON Schema dos parâmetros é construído com type=object, properties e required.
    /// </summary>
    private static FunctionDefinition BuildFunctionDefinition(ToolDefinition tool)
    {
        var properties = tool.Parameters.ToDictionary(
            p => p.Name,
            p => (object)new { type = p.Type.ToLowerInvariant(), description = p.Description });

        var required = tool.Parameters
            .Where(p => p.Required)
            .Select(p => p.Name)
            .ToArray();

        var parameters = new
        {
            type = "object",
            properties,
            required
        };

        return new FunctionDefinition(tool.Name, tool.Description, parameters);
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

    /// <summary>
    /// Gera um plano de execução breve via LLM para o modo de planeamento.
    /// Falha silenciosamente — se o plan não for gerado, a execução continua sem ele.
    /// </summary>
    private static async Task<string?> TryBuildExecutionPlanAsync(
        AiAgent agent,
        string input,
        IChatCompletionProvider chatProvider,
        ResolvedModel resolvedModel,
        CancellationToken cancellationToken)
    {
        try
        {
            var planPrompt = $"Given this task, list the steps you will take (be brief, 3-5 steps):\n{input}";
            var planRequest = new ChatCompletionRequest(
                resolvedModel.ModelName,
                [new ChatMessage("user", planPrompt)],
                Temperature: 0.2,
                MaxTokens: 256,
                SystemPrompt: $"You are a planning assistant for an AI agent named '{agent.DisplayName}'. " +
                              "Respond with a concise numbered step list only.");

            var planResult = await chatProvider.CompleteAsync(planRequest, cancellationToken);

            return planResult.Success && !string.IsNullOrWhiteSpace(planResult.Content)
                ? planResult.Content.Trim()
                : null;
        }
        catch
        {
            // Falha non-fatal — execução continua sem plano
            return null;
        }
    }

    private static string BuildSystemPrompt(AiAgent agent, IReadOnlyList<ToolDefinition> allowedTools, string? contextJson)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(agent.SystemPrompt))
            parts.Add(agent.SystemPrompt);

        if (!string.IsNullOrWhiteSpace(agent.Objective))
            parts.Add($"Objective: {agent.Objective}");

        if (!string.IsNullOrWhiteSpace(agent.OutputSchema))
            parts.Add($"Expected output format: {agent.OutputSchema}");

        // Inject grounding context when provided by the caller
        if (!string.IsNullOrWhiteSpace(contextJson))
        {
            parts.Add("## Grounding Context\nThe following operational context has been provided for this execution. " +
                       "Use it to ground your response with real data. If the context is insufficient, state limitations explicitly.\n\n" +
                       contextJson);
        }

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
