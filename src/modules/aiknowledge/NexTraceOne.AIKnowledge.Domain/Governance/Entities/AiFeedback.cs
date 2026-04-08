using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Feedback do utilizador sobre uma interação de IA (conversa, mensagem ou execução de agente).
/// Permite medir satisfação, identificar padrões negativos e alimentar
/// o ciclo de melhoria contínua dos modelos e agentes.
/// </summary>
public sealed class AiFeedback : AuditableEntity<AiFeedbackId>
{
    private AiFeedback() { }

    /// <summary>Identificador da conversa associada (null para execuções de agente).</summary>
    public Guid? ConversationId { get; private set; }

    /// <summary>Identificador da mensagem avaliada (null para execuções de agente).</summary>
    public Guid? MessageId { get; private set; }

    /// <summary>Identificador da execução de agente avaliada (null para conversas).</summary>
    public Guid? AgentExecutionId { get; private set; }

    /// <summary>Classificação do feedback: Positive, Negative ou Neutral.</summary>
    public FeedbackRating Rating { get; private set; }

    /// <summary>Comentário textual opcional do utilizador.</summary>
    public string? Comment { get; private set; }

    /// <summary>Nome do agente que produziu a resposta avaliada.</summary>
    public string AgentName { get; private set; } = string.Empty;

    /// <summary>Nome do modelo utilizado na interação avaliada.</summary>
    public string ModelUsed { get; private set; } = string.Empty;

    /// <summary>Categoria da consulta (ex: "incident-analysis", "contract-generation").</summary>
    public string? QueryCategory { get; private set; }

    /// <summary>Identificador do utilizador que submeteu o feedback.</summary>
    public string CreatedByUserId { get; private set; } = string.Empty;

    /// <summary>Identificador do tenant.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Timestamp UTC da submissão do feedback.</summary>
    public DateTimeOffset SubmittedAt { get; private set; }

    /// <summary>
    /// Regista um novo feedback do utilizador sobre uma interação de IA.
    /// </summary>
    public static AiFeedback Create(
        Guid? conversationId,
        Guid? messageId,
        Guid? agentExecutionId,
        FeedbackRating rating,
        string? comment,
        string agentName,
        string modelUsed,
        string? queryCategory,
        string createdByUserId,
        Guid tenantId,
        DateTimeOffset submittedAt)
    {
        Guard.Against.NullOrWhiteSpace(agentName);
        Guard.Against.NullOrWhiteSpace(modelUsed);
        Guard.Against.NullOrWhiteSpace(createdByUserId);
        Guard.Against.Default(tenantId);
        Guard.Against.EnumOutOfRange(rating);

        return new AiFeedback
        {
            Id = AiFeedbackId.New(),
            ConversationId = conversationId,
            MessageId = messageId,
            AgentExecutionId = agentExecutionId,
            Rating = rating,
            Comment = comment?.Trim(),
            AgentName = agentName.Trim(),
            ModelUsed = modelUsed.Trim(),
            QueryCategory = queryCategory?.Trim(),
            CreatedByUserId = createdByUserId,
            TenantId = tenantId,
            SubmittedAt = submittedAt
        };
    }
}

/// <summary>Identificador fortemente tipado de AiFeedback.</summary>
public sealed record AiFeedbackId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AiFeedbackId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AiFeedbackId From(Guid id) => new(id);
}
