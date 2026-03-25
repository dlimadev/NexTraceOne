using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Registo de uma execução de agent de IA.
/// Cada execução captura: agent usado, modelo, input/output, tokens e duração.
/// Auditável e imutável após conclusão.
///
/// Ciclo de vida: Pending → Running → (Completed | Failed | Cancelled).
///
/// Invariantes:
/// - AgentId é obrigatório.
/// - ExecutedBy é obrigatório.
/// - Status inicia em Pending.
/// - CompletedAt é preenchido apenas quando finaliza (Completed, Failed, Cancelled).
/// </summary>
public sealed class AiAgentExecution : AuditableEntity<AiAgentExecutionId>
{
    private AiAgentExecution() { }

    /// <summary>Agent que originou a execução.</summary>
    public AiAgentId AgentId { get; private set; } = null!;

    /// <summary>Utilizador que disparou a execução.</summary>
    public string ExecutedBy { get; private set; } = string.Empty;

    /// <summary>Estado actual da execução.</summary>
    public AgentExecutionStatus Status { get; private set; }

    /// <summary>Identificador do modelo utilizado.</summary>
    public Guid ModelIdUsed { get; private set; }

    /// <summary>Nome do provider utilizado.</summary>
    public string ProviderUsed { get; private set; } = string.Empty;

    /// <summary>Input fornecido ao agent (JSON).</summary>
    public string InputJson { get; private set; } = string.Empty;

    /// <summary>Output gerado pelo agent (JSON ou texto).</summary>
    public string OutputJson { get; private set; } = string.Empty;

    /// <summary>Tokens de prompt consumidos.</summary>
    public int PromptTokens { get; private set; }

    /// <summary>Tokens de completion consumidos.</summary>
    public int CompletionTokens { get; private set; }

    /// <summary>Total de tokens consumidos.</summary>
    public int TotalTokens { get; private set; }

    /// <summary>Duração da execução em milissegundos.</summary>
    public long DurationMs { get; private set; }

    /// <summary>Momento de início da execução.</summary>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>Momento de conclusão da execução (nulo se ainda em curso).</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Identificador de correlação para rastreabilidade.</summary>
    public string CorrelationId { get; private set; } = string.Empty;

    /// <summary>Mensagem de erro (apenas para Failed).</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Passos intermédios da execução (JSON array).</summary>
    public string Steps { get; private set; } = string.Empty;

    /// <summary>Contexto usado na execução (JSON).</summary>
    public string ContextJson { get; private set; } = string.Empty;

    /// <summary>Optimistic concurrency token (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>Cria uma nova execução de agent.</summary>
    public static AiAgentExecution Start(
        AiAgentId agentId,
        string executedBy,
        Guid modelIdUsed,
        string providerUsed,
        string inputJson,
        string? contextJson,
        DateTimeOffset startedAt,
        string? correlationId = null)
    {
        Guard.Against.Null(agentId);
        Guard.Against.NullOrWhiteSpace(executedBy);

        return new AiAgentExecution
        {
            Id = AiAgentExecutionId.New(),
            AgentId = agentId,
            ExecutedBy = executedBy,
            Status = AgentExecutionStatus.Running,
            ModelIdUsed = modelIdUsed,
            ProviderUsed = providerUsed ?? string.Empty,
            InputJson = inputJson ?? string.Empty,
            ContextJson = contextJson ?? string.Empty,
            StartedAt = startedAt,
            CorrelationId = correlationId ?? Guid.NewGuid().ToString("N"),
        };
    }

    /// <summary>Marca a execução como concluída com sucesso.</summary>
    public void Complete(
        string outputJson,
        int promptTokens,
        int completionTokens,
        long durationMs,
        DateTimeOffset completedAt,
        string? steps = null)
    {
        Status = AgentExecutionStatus.Completed;
        OutputJson = outputJson ?? string.Empty;
        PromptTokens = promptTokens;
        CompletionTokens = completionTokens;
        TotalTokens = promptTokens + completionTokens;
        DurationMs = durationMs;
        CompletedAt = completedAt;
        if (steps is not null) Steps = steps;
    }

    /// <summary>Marca a execução como falhada.</summary>
    public void Fail(string errorMessage, DateTimeOffset completedAt, long durationMs)
    {
        Status = AgentExecutionStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = completedAt;
        DurationMs = durationMs;
    }

    /// <summary>Marca a execução como cancelada.</summary>
    public void Cancel(DateTimeOffset completedAt)
    {
        Status = AgentExecutionStatus.Cancelled;
        CompletedAt = completedAt;
    }
}

/// <summary>Identificador fortemente tipado de AiAgentExecution.</summary>
public sealed record AiAgentExecutionId(Guid Value) : TypedIdBase(Value)
{
    public static AiAgentExecutionId New() => new(Guid.NewGuid());
    public static AiAgentExecutionId From(Guid id) => new(id);
}
