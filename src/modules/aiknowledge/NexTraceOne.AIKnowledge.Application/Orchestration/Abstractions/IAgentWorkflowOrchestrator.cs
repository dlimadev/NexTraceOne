using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Orchestration.Abstractions;

/// <summary>
/// Modelo de definição de um workflow multi-agent.
/// </summary>
public sealed record AgentWorkflowDefinition(
    string Name,
    IReadOnlyList<AgentWorkflowStep> Steps);

/// <summary>
/// Passo individual num workflow multi-agent.
/// </summary>
public sealed record AgentWorkflowStep(
    Guid AgentId,
    string? InputTemplate = null,
    int? ParallelGroupId = null,
    int? StepTimeoutSeconds = null);

/// <summary>
/// Resultado da execução de um passo do workflow.
/// </summary>
public sealed record AgentWorkflowStepResult(
    Guid AgentId,
    string AgentName,
    string Input,
    string Output,
    long DurationMs,
    bool Success,
    string? ErrorMessage = null,
    int RetryCount = 0);

/// <summary>
/// Resultado agregado de um workflow multi-agent.
/// </summary>
public sealed record AgentWorkflowResult(
    bool Success,
    IReadOnlyList<AgentWorkflowStepResult> StepResults,
    string? FinalOutput = null,
    string? ErrorMessage = null);

/// <summary>
/// Orquestrador de workflows multi-agent.
/// Executa agentes em sequência, paralelo ou condicional (futuro).
/// </summary>
public interface IAgentWorkflowOrchestrator
{
    /// <summary>
    /// Executa os agentes do workflow em sequência, passando o output de cada um
    /// como input do próximo (chain-of-thought style).
    /// </summary>
    Task<Result<AgentWorkflowResult>> ExecuteSequentialAsync(
        AgentWorkflowDefinition workflow,
        string initialInput,
        string? callerTeamId = null,
        bool enableAdaptiveReplanning = false,
        CancellationToken ct = default);
}
