using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de feedbacks de utilizadores sobre interações de IA.
/// Suporta consulta por conversa, classificação e agente para análise de satisfação.
/// </summary>
public interface IAiFeedbackRepository
{
    /// <summary>Adiciona um novo feedback para persistência.</summary>
    Task AddAsync(AiFeedback feedback, CancellationToken ct = default);

    /// <summary>Obtém um feedback pelo identificador.</summary>
    Task<AiFeedback?> GetByIdAsync(AiFeedbackId id, CancellationToken ct = default);

    /// <summary>Lista feedbacks associados a uma conversa.</summary>
    Task<IReadOnlyList<AiFeedback>> ListByConversationIdAsync(Guid conversationId, CancellationToken ct = default);

    /// <summary>Lista feedbacks com determinada classificação, limitados a um máximo.</summary>
    Task<IReadOnlyList<AiFeedback>> ListByRatingAsync(FeedbackRating rating, int limit, CancellationToken ct = default);

    /// <summary>Conta o total de feedbacks com determinada classificação.</summary>
    Task<int> CountByRatingAsync(FeedbackRating rating, CancellationToken ct = default);

    /// <summary>Lista feedbacks por nome de agente, limitados a um máximo.</summary>
    Task<IReadOnlyList<AiFeedback>> ListByAgentNameAsync(string agentName, int limit, CancellationToken ct = default);

    /// <summary>
    /// Conta feedbacks negativos para um agente e modelo específicos desde uma data.
    /// Usado pelo FeedbackThresholdJob para detetar acumulação de feedback negativo.
    /// </summary>
    Task<int> CountNegativeSinceAsync(
        string agentName,
        string modelUsed,
        DateTimeOffset since,
        CancellationToken ct = default);

    /// <summary>Calcula a média de rating para um agent específico, via AgentExecutionId.</summary>
    Task<double> GetAverageRatingAsync(Guid agentId, CancellationToken ct = default);
}
