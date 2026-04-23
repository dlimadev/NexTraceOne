using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Representa uma amostra de predição de um modelo de IA em produção.
/// Utilizada para detecção de drift de input/output e análise de qualidade de modelo.
///
/// Campos:
/// - <c>ModelId</c> / <c>ModelName</c> — identificação do modelo
/// - <c>ServiceId</c> — serviço que invocou o modelo
/// - <c>TenantId</c> — tenant proprietário
/// - <c>PredictedAt</c> — momento da predição
/// - <c>InputFeatureStatsJson</c> — JSON com estatísticas de input (mean/std/nullPct por feature)
/// - <c>PredictedClass</c> — classe predita (para classificação)
/// - <c>ConfidenceScore</c> — score de confiança [0,1]
/// - <c>InferenceLatencyMs</c> — latência de inferência em ms
/// - <c>ActualClass</c> — classe real (feedback loop, opcional)
/// - <c>IsFallback</c> — indica se a resposta foi um fallback
/// - <c>DriftAcknowledged</c> — indica se o drift foi reconhecido por um operador
///
/// Wave AT.1 — AI Model Quality &amp; Drift Governance.
/// </summary>
public sealed class ModelPredictionSample : AuditableEntity<ModelPredictionSampleId>
{
    private ModelPredictionSample() { }

    /// <summary>Identificador do modelo de IA.</summary>
    public Guid ModelId { get; private set; }

    /// <summary>Nome do modelo de IA (ex: "gpt-4o").</summary>
    public string ModelName { get; private set; } = string.Empty;

    /// <summary>Identificador do serviço que invocou o modelo.</summary>
    public string ServiceId { get; private set; } = string.Empty;

    /// <summary>Identificador do tenant proprietário.</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Momento em que a predição foi efectuada.</summary>
    public DateTimeOffset PredictedAt { get; private set; }

    /// <summary>Estatísticas de features de input serializado como JSON (mean/std/nullPct por feature).</summary>
    public string? InputFeatureStatsJson { get; private set; }

    /// <summary>Classe predita pelo modelo (para modelos de classificação).</summary>
    public string? PredictedClass { get; private set; }

    /// <summary>Score de confiança da predição [0,1].</summary>
    public double? ConfidenceScore { get; private set; }

    /// <summary>Latência de inferência em milissegundos.</summary>
    public int? InferenceLatencyMs { get; private set; }

    /// <summary>Classe real observada (feedback loop — opcional).</summary>
    public string? ActualClass { get; private set; }

    /// <summary>Indica se a resposta foi um fallback para resposta default.</summary>
    public bool IsFallback { get; private set; }

    /// <summary>Indica se um drift associado a esta amostra foi reconhecido por um operador.</summary>
    public bool DriftAcknowledged { get; private set; }

    /// <summary>
    /// Cria uma nova amostra de predição de modelo.
    /// </summary>
    public static ModelPredictionSample Create(
        Guid modelId,
        string modelName,
        string serviceId,
        string tenantId,
        DateTimeOffset predictedAt,
        string? inputFeatureStatsJson,
        string? predictedClass,
        double? confidenceScore,
        int? inferenceLatencyMs,
        string? actualClass,
        bool isFallback)
    {
        return new ModelPredictionSample
        {
            Id = ModelPredictionSampleId.New(),
            ModelId = modelId,
            ModelName = modelName,
            ServiceId = serviceId,
            TenantId = tenantId,
            PredictedAt = predictedAt,
            InputFeatureStatsJson = inputFeatureStatsJson,
            PredictedClass = predictedClass,
            ConfidenceScore = confidenceScore,
            InferenceLatencyMs = inferenceLatencyMs,
            ActualClass = actualClass,
            IsFallback = isFallback,
            DriftAcknowledged = false
        };
    }

    /// <summary>Regista o reconhecimento de drift por um operador.</summary>
    public void AcknowledgeDrift() => DriftAcknowledged = true;
}

/// <summary>Identificador fortemente tipado para <see cref="ModelPredictionSample"/>.</summary>
public sealed record ModelPredictionSampleId(Guid Value) : TypedIdBase(Value)
{
    public static ModelPredictionSampleId New() => new(Guid.NewGuid());
    public static ModelPredictionSampleId From(Guid id) => new(id);
}
