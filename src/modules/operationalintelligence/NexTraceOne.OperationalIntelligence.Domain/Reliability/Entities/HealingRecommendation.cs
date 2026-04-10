using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

/// <summary>
/// Recomendação de auto-recuperação gerada quando uma causa raiz é identificada para um incidente.
/// O sistema avalia padrões históricos e runbooks para sugerir acções concretas (restart, scale,
/// rollback, config change, circuit breaker, cache clear) que podem ser aprovadas e executadas
/// com trilha de auditoria completa.
///
/// Ciclo de vida: Proposed → Approved → Executing → Completed | Failed.
/// Alternativo: Proposed → Rejected.
///
/// Pilar: Operational Reliability + Self-Healing Recommendations.
/// Ideia 7 — Self-Healing Recommendations.
/// </summary>
public sealed class HealingRecommendation : AuditableEntity<HealingRecommendationId>
{
    private HealingRecommendation() { }

    /// <summary>Nome do serviço que precisa de recuperação.</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Ambiente onde o problema foi identificado.</summary>
    public string Environment { get; private set; } = string.Empty;

    /// <summary>Incidente associado (opcional).</summary>
    public Guid? IncidentId { get; private set; }

    /// <summary>Descrição da causa raiz identificada.</summary>
    public string RootCauseDescription { get; private set; } = string.Empty;

    /// <summary>Tipo de acção recomendada.</summary>
    public HealingActionType ActionType { get; private set; }

    /// <summary>Detalhes específicos da acção em formato JSONB.</summary>
    public string ActionDetails { get; private set; } = string.Empty;

    /// <summary>Grau de confiança da recomendação (0-100).</summary>
    public int ConfidenceScore { get; private set; }

    /// <summary>Impacto estimado da acção (JSONB, opcional).</summary>
    public string? EstimatedImpact { get; private set; }

    /// <summary>Lista de runbooks relacionados (JSONB, opcional).</summary>
    public string? RelatedRunbookIds { get; private set; }

    /// <summary>Taxa de sucesso histórica para acções semelhantes (0-100).</summary>
    public decimal? HistoricalSuccessRate { get; private set; }

    /// <summary>Estado da recomendação no ciclo de vida.</summary>
    public HealingRecommendationStatus Status { get; private set; } = HealingRecommendationStatus.Proposed;

    /// <summary>Identificador do utilizador que aprovou ou rejeitou.</summary>
    public string? ApprovedByUserId { get; private set; }

    /// <summary>Data/hora da aprovação ou rejeição.</summary>
    public DateTimeOffset? ApprovedAt { get; private set; }

    /// <summary>Data/hora de início da execução.</summary>
    public DateTimeOffset? ExecutionStartedAt { get; private set; }

    /// <summary>Data/hora de conclusão da execução.</summary>
    public DateTimeOffset? ExecutionCompletedAt { get; private set; }

    /// <summary>Resultado da execução (JSONB, opcional).</summary>
    public string? ExecutionResult { get; private set; }

    /// <summary>Mensagem de erro em caso de falha.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Trilha de evidências para auditoria (JSONB, opcional).</summary>
    public string? EvidenceTrail { get; private set; }

    /// <summary>Data/hora UTC em que a recomendação foi gerada.</summary>
    public DateTimeOffset GeneratedAt { get; private set; }

    /// <summary>Tenant ao qual pertence a recomendação.</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Token de concorrência optimista (xmin do PostgreSQL).</summary>
    public uint RowVersion { get; private set; }

    /// <summary>Cria uma nova recomendação de self-healing no estado Proposed.</summary>
    public static HealingRecommendation Generate(
        string serviceName,
        string environment,
        Guid? incidentId,
        string rootCauseDescription,
        HealingActionType actionType,
        string actionDetails,
        int confidenceScore,
        string? estimatedImpact,
        string? relatedRunbookIds,
        decimal? historicalSuccessRate,
        Guid? tenantId,
        DateTimeOffset generatedAt)
    {
        Guard.Against.NullOrWhiteSpace(serviceName, nameof(serviceName));
        Guard.Against.NullOrWhiteSpace(environment, nameof(environment));
        Guard.Against.NullOrWhiteSpace(rootCauseDescription, nameof(rootCauseDescription));
        Guard.Against.NullOrWhiteSpace(actionDetails, nameof(actionDetails));

        if (confidenceScore < 0 || confidenceScore > 100)
            throw new ArgumentException("Confidence score must be between 0 and 100.", nameof(confidenceScore));

        if (historicalSuccessRate.HasValue && (historicalSuccessRate.Value < 0 || historicalSuccessRate.Value > 100))
            throw new ArgumentException("Historical success rate must be between 0 and 100.", nameof(historicalSuccessRate));

        return new HealingRecommendation
        {
            Id = HealingRecommendationId.New(),
            ServiceName = serviceName,
            Environment = environment,
            IncidentId = incidentId,
            RootCauseDescription = rootCauseDescription,
            ActionType = actionType,
            ActionDetails = actionDetails,
            ConfidenceScore = confidenceScore,
            EstimatedImpact = estimatedImpact,
            RelatedRunbookIds = relatedRunbookIds,
            HistoricalSuccessRate = historicalSuccessRate,
            Status = HealingRecommendationStatus.Proposed,
            TenantId = tenantId,
            GeneratedAt = generatedAt,
        };
    }

    /// <summary>Aprova a recomendação — transição Proposed → Approved.</summary>
    public Result<Unit> Approve(string userId, DateTimeOffset approvedAt)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        if (Status != HealingRecommendationStatus.Proposed)
            return Error.Conflict(
                "Reliability.HealingRecommendation.InvalidTransition",
                $"Cannot transition healing recommendation '{Id.Value}' from '{Status}' to 'Approved'.");

        Status = HealingRecommendationStatus.Approved;
        ApprovedByUserId = userId;
        ApprovedAt = approvedAt;
        return Unit.Value;
    }

    /// <summary>Rejeita a recomendação — transição Proposed → Rejected.</summary>
    public Result<Unit> Reject(string userId, DateTimeOffset rejectedAt)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        if (Status != HealingRecommendationStatus.Proposed)
            return Error.Conflict(
                "Reliability.HealingRecommendation.InvalidTransition",
                $"Cannot transition healing recommendation '{Id.Value}' from '{Status}' to 'Rejected'.");

        Status = HealingRecommendationStatus.Rejected;
        ApprovedByUserId = userId;
        ApprovedAt = rejectedAt;
        return Unit.Value;
    }

    /// <summary>Inicia a execução da recomendação — transição Approved → Executing.</summary>
    public Result<Unit> StartExecution(DateTimeOffset startedAt)
    {
        if (Status != HealingRecommendationStatus.Approved)
            return Error.Conflict(
                "Reliability.HealingRecommendation.InvalidTransition",
                $"Cannot transition healing recommendation '{Id.Value}' from '{Status}' to 'Executing'.");

        Status = HealingRecommendationStatus.Executing;
        ExecutionStartedAt = startedAt;
        return Unit.Value;
    }

    /// <summary>Marca a execução como concluída com sucesso — transição Executing → Completed.</summary>
    public Result<Unit> CompleteExecution(string result, string evidence, DateTimeOffset completedAt)
    {
        Guard.Against.NullOrWhiteSpace(result, nameof(result));
        Guard.Against.NullOrWhiteSpace(evidence, nameof(evidence));

        if (Status != HealingRecommendationStatus.Executing)
            return Error.Conflict(
                "Reliability.HealingRecommendation.InvalidTransition",
                $"Cannot transition healing recommendation '{Id.Value}' from '{Status}' to 'Completed'.");

        Status = HealingRecommendationStatus.Completed;
        ExecutionResult = result;
        EvidenceTrail = evidence;
        ExecutionCompletedAt = completedAt;
        return Unit.Value;
    }

    /// <summary>Marca a execução como falhada — transição Executing → Failed.</summary>
    public Result<Unit> FailExecution(string error, DateTimeOffset completedAt)
    {
        Guard.Against.NullOrWhiteSpace(error, nameof(error));

        if (Status != HealingRecommendationStatus.Executing)
            return Error.Conflict(
                "Reliability.HealingRecommendation.InvalidTransition",
                $"Cannot transition healing recommendation '{Id.Value}' from '{Status}' to 'Failed'.");

        Status = HealingRecommendationStatus.Failed;
        ErrorMessage = error;
        ExecutionCompletedAt = completedAt;
        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de HealingRecommendation.</summary>
public sealed record HealingRecommendationId(Guid Value) : TypedIdBase(Value)
{
    public static HealingRecommendationId New() => new(Guid.NewGuid());
    public static HealingRecommendationId From(Guid id) => new(id);
}
