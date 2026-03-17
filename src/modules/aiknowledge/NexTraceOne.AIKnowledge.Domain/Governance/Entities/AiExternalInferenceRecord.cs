using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Registo de inferência realizada por IA externa, candidato a promoção
/// para a memória partilhada da organização.
/// Cada registo captura prompt, resposta, classificação de sensibilidade
/// e estado do fluxo de promoção de conhecimento.
///
/// Invariantes:
/// - UserId, TenantId, ProviderId e ModelName são obrigatórios.
/// - OriginalPrompt e Response são obrigatórios.
/// - QualityScore, quando presente, deve estar entre 1 e 5.
/// - PromotionStatus inicia como Pending.
/// - CanPromoteToSharedMemory inicia como false até aprovação explícita.
/// </summary>
public sealed class AiExternalInferenceRecord : AuditableEntity<AiExternalInferenceRecordId>
{
    private AiExternalInferenceRecord() { }

    /// <summary>Identificador do utilizador que originou a inferência.</summary>
    public string UserId { get; private set; } = string.Empty;

    /// <summary>Identificador do tenant do utilizador.</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Identificador do provedor de IA externo utilizado.</summary>
    public string ProviderId { get; private set; } = string.Empty;

    /// <summary>Nome do modelo externo utilizado.</summary>
    public string ModelName { get; private set; } = string.Empty;

    /// <summary>Prompt original enviado ao modelo externo.</summary>
    public string OriginalPrompt { get; private set; } = string.Empty;

    /// <summary>Contexto adicional enviado junto ao prompt (opcional).</summary>
    public string? AdditionalContext { get; private set; }

    /// <summary>Resposta gerada pelo modelo externo.</summary>
    public string Response { get; private set; } = string.Empty;

    /// <summary>Classificação de sensibilidade do conteúdo (ex: "public", "internal", "confidential").</summary>
    public string SensitivityClassification { get; private set; } = string.Empty;

    /// <summary>Pontuação de qualidade atribuída (1-5, null se não avaliado).</summary>
    public int? QualityScore { get; private set; }

    /// <summary>Estado atual do fluxo de promoção de conhecimento.</summary>
    public AiKnowledgePromotionStatus PromotionStatus { get; private set; }

    /// <summary>Indica se o registo pode ser promovido para a memória partilhada.</summary>
    public bool CanPromoteToSharedMemory { get; private set; }

    /// <summary>Data/hora UTC da revisão (null se não revisado).</summary>
    public DateTimeOffset? ReviewedAt { get; private set; }

    /// <summary>Identificador do revisor (null se não revisado).</summary>
    public string? ReviewedBy { get; private set; }

    /// <summary>
    /// Cria um novo registo de inferência externa com validações de invariantes.
    /// O registo inicia com PromotionStatus Pending e CanPromoteToSharedMemory false.
    /// </summary>
    public static AiExternalInferenceRecord Create(
        string userId,
        string tenantId,
        string providerId,
        string modelName,
        string originalPrompt,
        string? additionalContext,
        string response,
        string sensitivityClassification,
        int? qualityScore)
    {
        Guard.Against.NullOrWhiteSpace(userId);
        Guard.Against.NullOrWhiteSpace(tenantId);
        Guard.Against.NullOrWhiteSpace(providerId);
        Guard.Against.NullOrWhiteSpace(modelName);
        Guard.Against.NullOrWhiteSpace(originalPrompt);
        Guard.Against.NullOrWhiteSpace(response);
        Guard.Against.NullOrWhiteSpace(sensitivityClassification);

        if (qualityScore.HasValue)
            Guard.Against.OutOfRange(qualityScore.Value, nameof(qualityScore), 1, 5);

        return new AiExternalInferenceRecord
        {
            Id = AiExternalInferenceRecordId.New(),
            UserId = userId,
            TenantId = tenantId,
            ProviderId = providerId,
            ModelName = modelName,
            OriginalPrompt = originalPrompt,
            AdditionalContext = additionalContext,
            Response = response,
            SensitivityClassification = sensitivityClassification,
            QualityScore = qualityScore,
            PromotionStatus = AiKnowledgePromotionStatus.Pending,
            CanPromoteToSharedMemory = false,
            ReviewedAt = null,
            ReviewedBy = null
        };
    }

    /// <summary>
    /// Aprova o registo para promoção à memória partilhada.
    /// Define CanPromoteToSharedMemory como true e regista o revisor.
    /// </summary>
    public Result<Unit> Approve(string reviewedBy, DateTimeOffset reviewedAt)
    {
        Guard.Against.NullOrWhiteSpace(reviewedBy);

        PromotionStatus = AiKnowledgePromotionStatus.Approved;
        CanPromoteToSharedMemory = true;
        ReviewedAt = reviewedAt;
        ReviewedBy = reviewedBy;
        return Unit.Value;
    }

    /// <summary>
    /// Rejeita o registo — o conteúdo não será promovido.
    /// Define CanPromoteToSharedMemory como false e regista o revisor.
    /// </summary>
    public Result<Unit> Reject(string reviewedBy, DateTimeOffset reviewedAt)
    {
        Guard.Against.NullOrWhiteSpace(reviewedBy);

        PromotionStatus = AiKnowledgePromotionStatus.Rejected;
        CanPromoteToSharedMemory = false;
        ReviewedAt = reviewedAt;
        ReviewedBy = reviewedBy;
        return Unit.Value;
    }

    /// <summary>
    /// Marca o registo para revisão humana.
    /// Operação idempotente — não retorna erro se já em revisão.
    /// </summary>
    public Result<Unit> MarkForReview()
    {
        PromotionStatus = AiKnowledgePromotionStatus.UnderReview;
        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de AiExternalInferenceRecord.</summary>
public sealed record AiExternalInferenceRecordId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AiExternalInferenceRecordId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AiExternalInferenceRecordId From(Guid id) => new(id);
}
