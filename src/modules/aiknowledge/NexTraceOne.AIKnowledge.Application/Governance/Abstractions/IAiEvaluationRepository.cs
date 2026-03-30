using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de avaliações de qualidade de respostas de IA.
/// Suporta consulta por conversa, execução de agente, utilizador e período.
/// </summary>
public interface IAiEvaluationRepository
{
    /// <summary>Obtém uma avaliação pelo identificador.</summary>
    Task<AiEvaluation?> GetByIdAsync(AiEvaluationId id, CancellationToken ct = default);

    /// <summary>Lista avaliações por conversa.</summary>
    Task<IReadOnlyList<AiEvaluation>> GetByConversationAsync(Guid conversationId, CancellationToken ct = default);

    /// <summary>Lista avaliações por execução de agente.</summary>
    Task<IReadOnlyList<AiEvaluation>> GetByAgentExecutionAsync(Guid agentExecutionId, CancellationToken ct = default);

    /// <summary>Lista avaliações por utilizador.</summary>
    Task<IReadOnlyList<AiEvaluation>> GetByUserAsync(string userId, CancellationToken ct = default);

    /// <summary>Lista avaliações por tenant num período.</summary>
    Task<IReadOnlyList<AiEvaluation>> GetByTenantAndPeriodAsync(
        Guid tenantId,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken ct = default);

    /// <summary>Adiciona uma nova avaliação para persistência.</summary>
    Task AddAsync(AiEvaluation entity, CancellationToken ct = default);
}
