using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Avaliação de qualidade de prompts e respostas de IA.
/// Regista métricas de qualidade, relevância, precisão e utilidade
/// para cada interação, permitindo melhoria contínua.
///
/// Cada avaliação pode ser automática (gerada pelo sistema) ou manual
/// (feedback do utilizador), e está ligada a uma conversa ou execução de agente.
/// </summary>
public sealed class AiEvaluation : AuditableEntity<AiEvaluationId>
{
    private AiEvaluation() { }

    /// <summary>Tipo de avaliação: "automatic", "user_feedback", "peer_review".</summary>
    public string EvaluationType { get; private set; } = string.Empty;

    /// <summary>Identificador da conversa avaliada (null para execuções de agente).</summary>
    public Guid? ConversationId { get; private set; }

    /// <summary>Identificador da mensagem avaliada (null para execuções de agente).</summary>
    public Guid? MessageId { get; private set; }

    /// <summary>Identificador da execução de agente avaliada (null para conversas).</summary>
    public Guid? AgentExecutionId { get; private set; }

    /// <summary>Identificador do utilizador que criou a avaliação.</summary>
    public string UserId { get; private set; } = string.Empty;

    /// <summary>Identificador do tenant.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Nome do modelo utilizado na interação avaliada.</summary>
    public string ModelName { get; private set; } = string.Empty;

    /// <summary>Nome do template de prompt utilizado (null se prompt livre).</summary>
    public string? PromptTemplateName { get; private set; }

    /// <summary>Score de relevância (0.0 a 1.0).</summary>
    public decimal RelevanceScore { get; private set; }

    /// <summary>Score de precisão (0.0 a 1.0).</summary>
    public decimal AccuracyScore { get; private set; }

    /// <summary>Score de utilidade (0.0 a 1.0).</summary>
    public decimal UsefulnessScore { get; private set; }

    /// <summary>Score de segurança (0.0 a 1.0).</summary>
    public decimal SafetyScore { get; private set; }

    /// <summary>Score composto/geral (0.0 a 1.0).</summary>
    public decimal OverallScore { get; private set; }

    /// <summary>Feedback textual do utilizador ou do sistema.</summary>
    public string? Feedback { get; private set; }

    /// <summary>Tags de categorização (CSV, ex: "grounding-miss,hallucination").</summary>
    public string? Tags { get; private set; }

    /// <summary>Timestamp UTC da avaliação.</summary>
    public DateTimeOffset EvaluatedAt { get; private set; }

    /// <summary>
    /// Regista uma nova avaliação de qualidade.
    /// </summary>
    public static AiEvaluation Create(
        string evaluationType,
        Guid? conversationId,
        Guid? messageId,
        Guid? agentExecutionId,
        string userId,
        Guid tenantId,
        string modelName,
        string? promptTemplateName,
        decimal relevanceScore,
        decimal accuracyScore,
        decimal usefulnessScore,
        decimal safetyScore,
        decimal overallScore,
        string? feedback,
        string? tags,
        DateTimeOffset evaluatedAt)
    {
        Guard.Against.NullOrWhiteSpace(evaluationType);
        Guard.Against.NullOrWhiteSpace(userId);
        Guard.Against.Default(tenantId);
        Guard.Against.NullOrWhiteSpace(modelName);
        Guard.Against.OutOfRange(relevanceScore, nameof(relevanceScore), 0m, 1m);
        Guard.Against.OutOfRange(accuracyScore, nameof(accuracyScore), 0m, 1m);
        Guard.Against.OutOfRange(usefulnessScore, nameof(usefulnessScore), 0m, 1m);
        Guard.Against.OutOfRange(safetyScore, nameof(safetyScore), 0m, 1m);
        Guard.Against.OutOfRange(overallScore, nameof(overallScore), 0m, 1m);

        return new AiEvaluation
        {
            Id = AiEvaluationId.New(),
            EvaluationType = evaluationType.Trim(),
            ConversationId = conversationId,
            MessageId = messageId,
            AgentExecutionId = agentExecutionId,
            UserId = userId,
            TenantId = tenantId,
            ModelName = modelName.Trim(),
            PromptTemplateName = promptTemplateName?.Trim(),
            RelevanceScore = relevanceScore,
            AccuracyScore = accuracyScore,
            UsefulnessScore = usefulnessScore,
            SafetyScore = safetyScore,
            OverallScore = overallScore,
            Feedback = feedback?.Trim(),
            Tags = tags?.Trim(),
            EvaluatedAt = evaluatedAt
        };
    }
}

/// <summary>Identificador fortemente tipado de AiEvaluation.</summary>
public sealed record AiEvaluationId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AiEvaluationId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AiEvaluationId From(Guid id) => new(id);
}
