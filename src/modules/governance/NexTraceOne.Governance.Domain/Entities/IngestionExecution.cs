using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para IngestionExecution.
/// </summary>
public sealed record IngestionExecutionId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Representa uma execução de ingestão de dados.
/// Cada vez que um conector/fonte processa dados, uma execução é criada.
/// </summary>
public sealed class IngestionExecution : Entity<IngestionExecutionId>
{
    /// <summary>Conector que executou a ingestão.</summary>
    public IntegrationConnectorId ConnectorId { get; private init; } = null!;

    /// <summary>Fonte de dados da execução.</summary>
    public IngestionSourceId? SourceId { get; private init; }

    /// <summary>ID de correlação para rastreamento.</summary>
    public string? CorrelationId { get; private init; }

    /// <summary>Data/hora UTC de início da execução.</summary>
    public DateTimeOffset StartedAt { get; private init; }

    /// <summary>Data/hora UTC de fim da execução.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Duração em milissegundos.</summary>
    public long? DurationMs { get; private set; }

    /// <summary>Resultado da execução.</summary>
    public ExecutionResult Result { get; private set; }

    /// <summary>Total de itens processados.</summary>
    public int ItemsProcessed { get; private set; }

    /// <summary>Itens processados com sucesso.</summary>
    public int ItemsSucceeded { get; private set; }

    /// <summary>Itens com falha.</summary>
    public int ItemsFailed { get; private set; }

    /// <summary>Mensagem de erro, se aplicável.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Código de erro, se aplicável.</summary>
    public string? ErrorCode { get; private set; }

    /// <summary>Number of retry attempts for this execution (0 = first attempt).</summary>
    public int RetryAttempt { get; private set; }

    /// <summary>Data/hora UTC de criação.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    private IngestionExecution() { }

    /// <summary>
    /// Inicia uma nova execução de ingestão.
    /// </summary>
    public static IngestionExecution Start(
        IntegrationConnectorId connectorId,
        IngestionSourceId? sourceId,
        string? correlationId,
        DateTimeOffset utcNow,
        int retryAttempt = 0)
    {
        Guard.Against.Null(connectorId, nameof(connectorId));

        return new IngestionExecution
        {
            Id = new IngestionExecutionId(Guid.NewGuid()),
            ConnectorId = connectorId,
            SourceId = sourceId,
            CorrelationId = correlationId ?? $"exec-{Guid.NewGuid():N}"[..20],
            StartedAt = utcNow,
            Result = ExecutionResult.Running,
            RetryAttempt = retryAttempt,
            CreatedAt = utcNow
        };
    }

    /// <summary>Marca a execução como concluída com sucesso.</summary>
    public void CompleteSuccess(int itemsProcessed, int itemsSucceeded, DateTimeOffset utcNow)
    {
        CompletedAt = utcNow;
        DurationMs = (long)(CompletedAt.Value - StartedAt).TotalMilliseconds;
        Result = ExecutionResult.Success;
        ItemsProcessed = itemsProcessed;
        ItemsSucceeded = itemsSucceeded;
        ItemsFailed = 0;
    }

    /// <summary>Marca a execução como parcialmente bem-sucedida.</summary>
    public void CompletePartialSuccess(
        int itemsProcessed,
        int itemsSucceeded,
        int itemsFailed,
        DateTimeOffset utcNow)
    {
        CompletedAt = utcNow;
        DurationMs = (long)(CompletedAt.Value - StartedAt).TotalMilliseconds;
        Result = ExecutionResult.PartialSuccess;
        ItemsProcessed = itemsProcessed;
        ItemsSucceeded = itemsSucceeded;
        ItemsFailed = itemsFailed;
    }

    /// <summary>Marca a execução como falha.</summary>
    public void CompleteFailed(string errorMessage, string? errorCode, DateTimeOffset utcNow)
    {
        Guard.Against.NullOrWhiteSpace(errorMessage, nameof(errorMessage));

        CompletedAt = utcNow;
        DurationMs = (long)(CompletedAt.Value - StartedAt).TotalMilliseconds;
        Result = ExecutionResult.Failed;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
    }
}
