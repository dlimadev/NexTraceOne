using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

/// <summary>
/// Padrão preditivo de incidentes identificado a partir de análise de dados históricos.
/// Representa correlações descobertas entre mudanças, deploys, serviços e incidentes
/// que podem prever incidentes futuros.
///
/// Exemplos: "Deploy na sexta-feira no serviço X gera incidente 60% das vezes",
/// "Mudança em contrato Y sem testes causa incidente em 48h".
///
/// Ciclo de vida: Detected → (Confirmed | Dismissed | Stale).
/// Fica Stale quando os dados subjacentes mudam significativamente.
///
/// Pilar: Operational Intelligence.
/// Ideia 12 — Predictive Incident Prevention.
/// </summary>
public sealed class IncidentPredictionPattern : AuditableEntity<IncidentPredictionPatternId>
{
    private IncidentPredictionPattern() { }

    /// <summary>Nome descritivo do padrão identificado.</summary>
    public string PatternName { get; private set; } = string.Empty;

    /// <summary>Descrição detalhada do padrão em linguagem natural.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Tipo de padrão identificado (ex: DeployTiming, ContractChange, ServiceCorrelation).</summary>
    public PredictionPatternType PatternType { get; private set; }

    /// <summary>Serviço principal associado ao padrão (opcional).</summary>
    public string? ServiceId { get; private set; }

    /// <summary>Nome do serviço associado (opcional).</summary>
    public string? ServiceName { get; private set; }

    /// <summary>Ambiente onde o padrão foi observado.</summary>
    public string Environment { get; private set; } = string.Empty;

    /// <summary>Confiança do padrão (0-100).</summary>
    public int ConfidencePercent { get; private set; }

    /// <summary>Número de ocorrências históricas que suportam o padrão.</summary>
    public int OccurrenceCount { get; private set; }

    /// <summary>Número total de observações analisadas para determinar o padrão.</summary>
    public int SampleSize { get; private set; }

    /// <summary>Evidências históricas que suportam o padrão (JSONB).</summary>
    public string Evidence { get; private set; } = string.Empty;

    /// <summary>Condições de trigger do padrão (JSONB) — quando o padrão se activa.</summary>
    public string TriggerConditions { get; private set; } = string.Empty;

    /// <summary>Recomendações de prevenção (JSONB).</summary>
    public string? PreventionRecommendations { get; private set; }

    /// <summary>Severidade estimada do incidente previsto.</summary>
    public PredictionSeverity Severity { get; private set; }

    /// <summary>Estado do padrão no ciclo de vida.</summary>
    public PredictionPatternStatus Status { get; private set; } = PredictionPatternStatus.Detected;

    /// <summary>Data/hora UTC da última vez que o padrão foi detectado.</summary>
    public DateTimeOffset DetectedAt { get; private set; }

    /// <summary>Data/hora UTC da última validação (se confirmado ou dismissed).</summary>
    public DateTimeOffset? ValidatedAt { get; private set; }

    /// <summary>Comentário de validação.</summary>
    public string? ValidationComment { get; private set; }

    /// <summary>Tenant ao qual pertence o padrão.</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Cria um novo padrão preditivo de incidentes.</summary>
    public static IncidentPredictionPattern Detect(
        string patternName,
        string description,
        PredictionPatternType patternType,
        string? serviceId,
        string? serviceName,
        string environment,
        int confidencePercent,
        int occurrenceCount,
        int sampleSize,
        string evidence,
        string triggerConditions,
        string? preventionRecommendations,
        PredictionSeverity severity,
        Guid? tenantId,
        DateTimeOffset detectedAt)
    {
        Guard.Against.NullOrWhiteSpace(patternName, nameof(patternName));
        Guard.Against.NullOrWhiteSpace(description, nameof(description));
        Guard.Against.NullOrWhiteSpace(environment, nameof(environment));
        Guard.Against.NullOrWhiteSpace(evidence, nameof(evidence));
        Guard.Against.NullOrWhiteSpace(triggerConditions, nameof(triggerConditions));

        if (confidencePercent < 0 || confidencePercent > 100)
            throw new ArgumentException("Confidence percent must be between 0 and 100.", nameof(confidencePercent));

        if (occurrenceCount < 0)
            throw new ArgumentException("Occurrence count cannot be negative.", nameof(occurrenceCount));

        if (sampleSize < 0)
            throw new ArgumentException("Sample size cannot be negative.", nameof(sampleSize));

        if (occurrenceCount > sampleSize)
            throw new ArgumentException("Occurrence count cannot exceed sample size.", nameof(occurrenceCount));

        return new IncidentPredictionPattern
        {
            Id = IncidentPredictionPatternId.New(),
            PatternName = patternName,
            Description = description,
            PatternType = patternType,
            ServiceId = serviceId,
            ServiceName = serviceName,
            Environment = environment,
            ConfidencePercent = confidencePercent,
            OccurrenceCount = occurrenceCount,
            SampleSize = sampleSize,
            Evidence = evidence,
            TriggerConditions = triggerConditions,
            PreventionRecommendations = preventionRecommendations,
            Severity = severity,
            Status = PredictionPatternStatus.Detected,
            TenantId = tenantId,
            DetectedAt = detectedAt
        };
    }

    /// <summary>Confirma o padrão como válido para prevenção ativa.</summary>
    public Result<Unit> Confirm(string comment, DateTimeOffset validatedAt)
    {
        Guard.Against.NullOrWhiteSpace(comment, nameof(comment));

        if (Status == PredictionPatternStatus.Confirmed)
            return Error.Conflict("PREDICTION_PATTERN_ALREADY_CONFIRMED",
                $"Prediction pattern '{Id.Value}' has already been confirmed.");

        Status = PredictionPatternStatus.Confirmed;
        ValidationComment = comment;
        ValidatedAt = validatedAt;
        return Unit.Value;
    }

    /// <summary>Descarta o padrão como falso positivo ou irrelevante.</summary>
    public Result<Unit> Dismiss(string comment, DateTimeOffset validatedAt)
    {
        Guard.Against.NullOrWhiteSpace(comment, nameof(comment));

        if (Status == PredictionPatternStatus.Dismissed)
            return Error.Conflict("PREDICTION_PATTERN_ALREADY_DISMISSED",
                $"Prediction pattern '{Id.Value}' has already been dismissed.");

        Status = PredictionPatternStatus.Dismissed;
        ValidationComment = comment;
        ValidatedAt = validatedAt;
        return Unit.Value;
    }

    /// <summary>Marca o padrão como stale (dados subjacentes mudaram).</summary>
    public Result<Unit> MarkAsStale()
    {
        if (Status == PredictionPatternStatus.Stale)
            return Error.Conflict("PREDICTION_PATTERN_ALREADY_STALE",
                $"Prediction pattern '{Id.Value}' is already stale.");

        Status = PredictionPatternStatus.Stale;
        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de IncidentPredictionPattern.</summary>
public sealed record IncidentPredictionPatternId(Guid Value) : TypedIdBase(Value)
{
    public static IncidentPredictionPatternId New() => new(Guid.NewGuid());
    public static IncidentPredictionPatternId From(Guid id) => new(id);
}
