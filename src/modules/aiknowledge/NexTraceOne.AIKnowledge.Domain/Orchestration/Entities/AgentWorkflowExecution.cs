using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;

/// <summary>
/// Registo de execução de um workflow multi-agent.
/// Captura: nome do workflow, steps executados, input/output, duração e retry count.
/// Auditável e imutável após conclusão.
///
/// Ciclo de vida: Pending → Running → (Completed | Failed | Cancelled).
/// </summary>
public sealed class AgentWorkflowExecution : AuditableEntity<AgentWorkflowExecutionId>
{
    private AgentWorkflowExecution() { }

    /// <summary>Nome do workflow executado.</summary>
    public string WorkflowName { get; private set; } = string.Empty;

    /// <summary>Estado actual da execução.</summary>
    public AgentExecutionStatus Status { get; private set; }

    /// <summary>Input inicial fornecido ao workflow.</summary>
    public string InitialInput { get; private set; } = string.Empty;

    /// <summary>Output final do workflow (após último step).</summary>
    public string FinalOutput { get; private set; } = string.Empty;

    /// <summary>Resultado de cada step em JSON array.</summary>
    public string StepResultsJson { get; private set; } = "[]";

    /// <summary>Número total de steps no workflow.</summary>
    public int TotalSteps { get; private set; }

    /// <summary>Número de steps que concluíram com sucesso.</summary>
    public int SuccessfulSteps { get; private set; }

    /// <summary>Número total de retries executados.</summary>
    public int TotalRetries { get; private set; }

    /// <summary>Duração total da execução em milissegundos.</summary>
    public long DurationMs { get; private set; }

    /// <summary>Momento de início da execução.</summary>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>Momento de conclusão (nulo se ainda em curso).</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Identificador de correlação para rastreabilidade.</summary>
    public string CorrelationId { get; private set; } = string.Empty;

    /// <summary>Utilizador/equipe que iniciou o workflow.</summary>
    public string? CallerTeamId { get; private set; }

    /// <summary>Mensagem de erro (apenas para Failed).</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Optimistic concurrency token.</summary>
    public uint RowVersion { get; private set; }

    /// <summary>Cria uma nova execução de workflow.</summary>
    public static AgentWorkflowExecution Start(
        string workflowName,
        string initialInput,
        int totalSteps,
        DateTimeOffset startedAt,
        string? callerTeamId = null,
        string? correlationId = null)
    {
        Guard.Against.NullOrWhiteSpace(workflowName);

        return new AgentWorkflowExecution
        {
            Id = AgentWorkflowExecutionId.New(),
            WorkflowName = workflowName,
            Status = AgentExecutionStatus.Running,
            InitialInput = initialInput ?? string.Empty,
            TotalSteps = totalSteps,
            StartedAt = startedAt,
            CallerTeamId = callerTeamId,
            CorrelationId = correlationId ?? Guid.NewGuid().ToString("N"),
        };
    }

    /// <summary>Regista resultados de steps e final output.</summary>
    public void RecordProgress(
        string stepResultsJson,
        int successfulSteps,
        int totalRetries,
        string finalOutput)
    {
        StepResultsJson = stepResultsJson ?? "[]";
        SuccessfulSteps = successfulSteps;
        TotalRetries = totalRetries;
        FinalOutput = finalOutput ?? string.Empty;
    }

    /// <summary>Marca a execução como concluída com sucesso.</summary>
    public void Complete(long durationMs, DateTimeOffset completedAt)
    {
        Status = AgentExecutionStatus.Completed;
        DurationMs = durationMs;
        CompletedAt = completedAt;
    }

    /// <summary>Marca a execução como falhada.</summary>
    public void Fail(string errorMessage, long durationMs, DateTimeOffset completedAt)
    {
        Status = AgentExecutionStatus.Failed;
        ErrorMessage = errorMessage;
        DurationMs = durationMs;
        CompletedAt = completedAt;
    }

    /// <summary>Marca a execução como cancelada.</summary>
    public void Cancel(DateTimeOffset completedAt)
    {
        Status = AgentExecutionStatus.Cancelled;
        CompletedAt = completedAt;
    }
}

/// <summary>Identificador fortemente tipado de AgentWorkflowExecution.</summary>
public sealed record AgentWorkflowExecutionId(Guid Value) : TypedIdBase(Value)
{
    public static AgentWorkflowExecutionId New() => new(Guid.NewGuid());
    public static AgentWorkflowExecutionId From(Guid id) => new(id);
}
