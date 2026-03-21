using System.Diagnostics;

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
/// Orquestra o pipeline: agent → modelo → provider → inferência → artefactos.
///
/// Pipeline de execução:
/// 1. Resolve agent por ID
/// 2. Valida agent activo
/// 3. Valida acesso do utilizador ao agent
/// 4. Resolve modelo (override ou preferred do agent)
/// 5. Valida modelo permitido para o agent
/// 6. Resolve provider
/// 7. Monta prompt (system + input)
/// 8. Executa inferência
/// 9. Persiste execução
/// 10. Gera artefactos (se aplicável)
/// 11. Persiste artefactos
/// 12. Retorna resultado
/// </summary>
public sealed class AiAgentRuntimeService(
    IAiAgentRepository agentRepository,
    IAiAgentExecutionRepository executionRepository,
    IAiAgentArtifactRepository artifactRepository,
    IAiModelCatalogService modelCatalogService,
    IAiProviderFactory providerFactory,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider) : IAiAgentRuntimeService
{
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

        // 7. Inicia execução
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

        // 8. Monta prompt e executa inferência
        var systemPrompt = BuildSystemPrompt(agent);
        var messages = new List<ChatCompletionRequest>(1);

        var chatMessages = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
            chatMessages.Add(new ChatMessage("system", systemPrompt));
        chatMessages.Add(new ChatMessage("user", input));

        var sw = Stopwatch.StartNew();
        ChatCompletionResult chatResult;

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

        // 9. Conclui execução
        execution.Complete(
            chatResult.Content ?? string.Empty,
            chatResult.PromptTokens,
            chatResult.CompletionTokens,
            sw.ElapsedMilliseconds,
            dateTimeProvider.UtcNow);

        await executionRepository.UpdateAsync(execution, cancellationToken);

        // 10. Incrementa contador
        agent.IncrementExecutionCount();
        await agentRepository.UpdateAsync(agent, cancellationToken);

        // 11. Gera artefactos baseado no tipo de agent
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
            artifacts);
    }

    private static string BuildSystemPrompt(AiAgent agent)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(agent.SystemPrompt))
            parts.Add(agent.SystemPrompt);

        if (!string.IsNullOrWhiteSpace(agent.Objective))
            parts.Add($"Objective: {agent.Objective}");

        if (!string.IsNullOrWhiteSpace(agent.OutputSchema))
            parts.Add($"Expected output format: {agent.OutputSchema}");

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
