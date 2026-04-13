using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Serviço de runtime de agents. Orquestra a execução de um agent:
/// resolução → validação → inferência → persistência → artefactos.
/// </summary>
public interface IAiAgentRuntimeService
{
    /// <summary>Executa um agent com input e modelo opcionais.</summary>
    Task<Result<AgentExecutionResult>> ExecuteAsync(
        AiAgentId agentId,
        string input,
        Guid? modelIdOverride,
        string? contextJson,
        string? callerTeamId,
        CancellationToken cancellationToken);
}

/// <summary>Resultado de uma execução de agent.</summary>
public sealed record AgentExecutionResult(
    Guid ExecutionId,
    Guid AgentId,
    string AgentName,
    string Status,
    string Output,
    int PromptTokens,
    int CompletionTokens,
    long DurationMs,
    IReadOnlyList<AgentArtifactResult> Artifacts,
    IReadOnlyList<ToolExecutionSummary>? ToolExecutions = null);

/// <summary>Artefacto produzido pela execução.</summary>
public sealed record AgentArtifactResult(
    Guid ArtifactId,
    string ArtifactType,
    string Title,
    string Format);

/// <summary>Resumo de uma execução de tool durante o pipeline do agent.</summary>
public sealed record ToolExecutionSummary(
    string ToolName,
    bool Success,
    long DurationMs,
    string? ErrorMessage = null);
