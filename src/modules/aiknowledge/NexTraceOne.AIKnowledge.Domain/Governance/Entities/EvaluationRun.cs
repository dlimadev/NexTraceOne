using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Execução de uma suite de avaliação contra um modelo e versão de prompt específicos.
/// Persiste resultados completos e métricas agregadas para auditoria e comparação.
/// </summary>
public sealed class EvaluationRun : AuditableEntity<EvaluationRunId>
{
    private EvaluationRun() { }

    /// <summary>Suite de avaliação executada.</summary>
    public EvaluationSuiteId SuiteId { get; private set; } = null!;

    /// <summary>Modelo avaliado nesta run.</summary>
    public Guid ModelId { get; private set; }

    /// <summary>Versão do prompt utilizada.</summary>
    public string PromptVersion { get; private set; } = string.Empty;

    /// <summary>Estado da execução.</summary>
    public EvaluationRunStatus Status { get; private set; }

    /// <summary>Timestamp de início da execução.</summary>
    public DateTimeOffset? StartedAt { get; private set; }

    /// <summary>Timestamp de conclusão da execução.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Total de casos executados.</summary>
    public int TotalCases { get; private set; }

    /// <summary>Casos que passaram nos critérios de avaliação.</summary>
    public int PassedCases { get; private set; }

    /// <summary>Casos que falharam nos critérios de avaliação.</summary>
    public int FailedCases { get; private set; }

    /// <summary>Latência média em milissegundos por caso.</summary>
    public double AverageLatencyMs { get; private set; }

    /// <summary>Custo total de tokens desta run.</summary>
    public decimal TotalTokenCost { get; private set; }

    /// <summary>Tenant proprietário desta run.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Cria uma nova run de avaliação no estado Pending.</summary>
    public static EvaluationRun Create(
        EvaluationSuiteId suiteId,
        Guid modelId,
        string promptVersion,
        Guid tenantId) => new()
    {
        Id = EvaluationRunId.New(),
        SuiteId = Guard.Against.Null(suiteId),
        ModelId = modelId,
        PromptVersion = Guard.Against.NullOrWhiteSpace(promptVersion),
        TenantId = tenantId,
        Status = EvaluationRunStatus.Pending
    };

    /// <summary>Inicia a execução.</summary>
    public void Start(DateTimeOffset startedAt)
    {
        Status = EvaluationRunStatus.Running;
        StartedAt = startedAt;
    }

    /// <summary>Conclui a execução com métricas agregadas.</summary>
    public void Complete(int passed, int failed, double avgLatencyMs, decimal tokenCost, DateTimeOffset completedAt)
    {
        Status = EvaluationRunStatus.Completed;
        PassedCases = passed;
        FailedCases = failed;
        TotalCases = passed + failed;
        AverageLatencyMs = avgLatencyMs;
        TotalTokenCost = tokenCost;
        CompletedAt = completedAt;
    }

    /// <summary>Marca a execução como falhada.</summary>
    public void Fail(DateTimeOffset failedAt)
    {
        Status = EvaluationRunStatus.Failed;
        CompletedAt = failedAt;
    }
}

/// <summary>Identificador fortemente tipado de EvaluationRun.</summary>
public sealed record EvaluationRunId(Guid Value) : TypedIdBase(Value)
{
    public static EvaluationRunId New() => new(Guid.NewGuid());
    public static EvaluationRunId From(Guid id) => new(id);
}
