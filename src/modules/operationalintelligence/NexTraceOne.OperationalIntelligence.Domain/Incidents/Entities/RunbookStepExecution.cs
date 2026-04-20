using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;

/// <summary>
/// Entidade que representa a execução de um passo de runbook operacional.
/// Regista quem executou, quando, qual o estado e o resultado da execução.
/// </summary>
public sealed class RunbookStepExecution
{
    private RunbookStepExecution() { }

    /// <summary>Identificador único da execução do passo.</summary>
    public RunbookStepExecutionId Id { get; private set; } = null!;

    /// <summary>Identificador do runbook ao qual este passo pertence.</summary>
    public Guid RunbookId { get; private set; }

    /// <summary>Chave identificadora do passo executado (ex: "step-1", "drain-connections").</summary>
    public string StepKey { get; private set; } = string.Empty;

    /// <summary>Identificador do utilizador que executou o passo.</summary>
    public string ExecutorUserId { get; private set; } = string.Empty;

    /// <summary>Estado corrente da execução do passo.</summary>
    public RunbookStepExecutionStatus ExecutionStatus { get; private set; }

    /// <summary>Data/hora de início da execução.</summary>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>Data/hora de conclusão da execução (null se ainda em andamento).</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Resumo textual do resultado da execução bem-sucedida.</summary>
    public string? OutputSummary { get; private set; }

    /// <summary>Detalhe do erro em caso de falha na execução.</summary>
    public string? ErrorDetail { get; private set; }

    /// <summary>Tenant associado à execução (null em contextos sem isolamento de tenant).</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>
    /// Factory method que cria uma nova execução de passo de runbook no estado Pending.
    /// </summary>
    public static RunbookStepExecution Create(
        Guid runbookId,
        string stepKey,
        string executorUserId,
        DateTimeOffset startedAt,
        Guid? tenantId = null)
    {
        Guard.Against.Default(runbookId);
        Guard.Against.NullOrWhiteSpace(stepKey);
        Guard.Against.NullOrWhiteSpace(executorUserId);

        return new RunbookStepExecution
        {
            Id = RunbookStepExecutionId.New(),
            RunbookId = runbookId,
            StepKey = stepKey,
            ExecutorUserId = executorUserId,
            ExecutionStatus = RunbookStepExecutionStatus.Pending,
            StartedAt = startedAt,
            TenantId = tenantId,
        };
    }

    /// <summary>Marca a execução como bem-sucedida com o resumo de saída fornecido.</summary>
    public void MarkSucceeded(string? outputSummary, DateTimeOffset completedAt)
    {
        ExecutionStatus = RunbookStepExecutionStatus.Succeeded;
        OutputSummary = outputSummary;
        CompletedAt = completedAt;
    }

    /// <summary>Marca a execução como falhada com o detalhe do erro fornecido.</summary>
    public void MarkFailed(string? errorDetail, DateTimeOffset completedAt)
    {
        ExecutionStatus = RunbookStepExecutionStatus.Failed;
        ErrorDetail = errorDetail;
        CompletedAt = completedAt;
    }
}

/// <summary>Identificador fortemente tipado de RunbookStepExecution.</summary>
public sealed record RunbookStepExecutionId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static RunbookStepExecutionId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static RunbookStepExecutionId From(Guid id) => new(id);
}

/// <summary>
/// Estado de ciclo de vida da execução de um passo de runbook.
/// </summary>
public enum RunbookStepExecutionStatus
{
    /// <summary>Execução criada — aguarda início.</summary>
    Pending = 0,

    /// <summary>Execução em andamento.</summary>
    Running = 1,

    /// <summary>Execução concluída com sucesso.</summary>
    Succeeded = 2,

    /// <summary>Execução falhada — ver ErrorDetail.</summary>
    Failed = 3,

    /// <summary>Passo ignorado (skipped) durante a execução do runbook.</summary>
    Skipped = 4
}
