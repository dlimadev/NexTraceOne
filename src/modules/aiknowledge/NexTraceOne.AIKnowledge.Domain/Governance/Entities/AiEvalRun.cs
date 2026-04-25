using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Identificador fortemente tipado para AiEvalRun.
/// </summary>
public sealed record AiEvalRunId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Estado de uma execução de avaliação de modelo IA.
/// </summary>
public enum AiEvalRunStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3
}

/// <summary>
/// Execução de avaliação de um dataset contra um modelo IA específico.
/// Regista métricas agregadas: ExactMatch, SemanticSimilarity, ToolCallAccuracy,
/// LatencyP50Ms, LatencyP95Ms, TotalTokenCost.
///
/// Cada run é associado a um AiEvalDataset e um modelo (ex: "claude-opus-4-7", "claude-sonnet-4-6").
/// Permite comparação histórica de qualidade por caso de uso.
///
/// Referência: CC-05, ADR-009.
/// Owner: módulo AIKnowledge (Governance).
/// </summary>
public sealed class AiEvalRun : Entity<AiEvalRunId>
{
    /// <summary>Tenant proprietário.</summary>
    public string TenantId { get; private init; } = string.Empty;

    /// <summary>Dataset de avaliação usado nesta run.</summary>
    public Guid DatasetId { get; private init; }

    /// <summary>Identificador do modelo avaliado (ex: "claude-opus-4-7").</summary>
    public string ModelId { get; private set; } = string.Empty;

    /// <summary>Estado da execução.</summary>
    public AiEvalRunStatus Status { get; private set; }

    /// <summary>Número de casos de teste processados.</summary>
    public int CasesProcessed { get; private set; }

    /// <summary>Número de casos com exact match (saída idêntica à esperada).</summary>
    public int ExactMatchCount { get; private set; }

    /// <summary>Similaridade semântica média (0.0–1.0, cosine sobre embeddings).</summary>
    public decimal AverageSemanticSimilarity { get; private set; }

    /// <summary>Precisão de tool calls (ratio de tool calls corretos/total).</summary>
    public decimal ToolCallAccuracy { get; private set; }

    /// <summary>Latência P50 em milissegundos.</summary>
    public double LatencyP50Ms { get; private set; }

    /// <summary>Latência P95 em milissegundos.</summary>
    public double LatencyP95Ms { get; private set; }

    /// <summary>Custo total em tokens (input + output tokens).</summary>
    public long TotalTokenCost { get; private set; }

    /// <summary>Mensagem de erro se a run falhou.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Data/hora UTC de início.</summary>
    public DateTimeOffset StartedAt { get; private init; }

    /// <summary>Data/hora UTC de conclusão.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    private AiEvalRun() { }

    /// <summary>Cria uma nova execução de avaliação.</summary>
    public static AiEvalRun Create(
        string tenantId,
        Guid datasetId,
        string modelId,
        DateTimeOffset utcNow)
    {
        Guard.Against.NullOrWhiteSpace(tenantId, nameof(tenantId));
        Guard.Against.Default(datasetId, nameof(datasetId));
        Guard.Against.NullOrWhiteSpace(modelId, nameof(modelId));
        Guard.Against.StringTooLong(modelId, 100, nameof(modelId));

        return new AiEvalRun
        {
            Id = new AiEvalRunId(Guid.NewGuid()),
            TenantId = tenantId,
            DatasetId = datasetId,
            ModelId = modelId.Trim(),
            Status = AiEvalRunStatus.Pending,
            StartedAt = utcNow
        };
    }

    /// <summary>Marca o início do processamento.</summary>
    public void Start() => Status = AiEvalRunStatus.Running;

    /// <summary>Regista os resultados e completa a run.</summary>
    public void Complete(
        int casesProcessed,
        int exactMatchCount,
        decimal averageSemanticSimilarity,
        decimal toolCallAccuracy,
        double latencyP50Ms,
        double latencyP95Ms,
        long totalTokenCost,
        DateTimeOffset utcNow)
    {
        Status = AiEvalRunStatus.Completed;
        CasesProcessed = casesProcessed;
        ExactMatchCount = exactMatchCount;
        AverageSemanticSimilarity = Math.Clamp(averageSemanticSimilarity, 0m, 1m);
        ToolCallAccuracy = Math.Clamp(toolCallAccuracy, 0m, 1m);
        LatencyP50Ms = latencyP50Ms;
        LatencyP95Ms = latencyP95Ms;
        TotalTokenCost = totalTokenCost;
        CompletedAt = utcNow;
    }

    /// <summary>Marca a run como falhada.</summary>
    public void Fail(string errorMessage, DateTimeOffset utcNow)
    {
        Status = AiEvalRunStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = utcNow;
    }
}
