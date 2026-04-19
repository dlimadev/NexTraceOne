using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de feedback sobre execuções de skills de IA.
/// Alimenta o ciclo de melhoria contínua (Agent Lightning RL).
/// </summary>
public interface IAiSkillFeedbackRepository
{
    /// <summary>Lista feedbacks de uma execução específica.</summary>
    Task<IReadOnlyList<AiSkillFeedback>> ListByExecutionAsync(AiSkillExecutionId executionId, CancellationToken ct);

    /// <summary>Calcula a classificação média de uma skill com base em todo o feedback recebido.</summary>
    Task<double?> GetAverageRatingBySkillAsync(AiSkillId skillId, CancellationToken ct);

    /// <summary>Adiciona novo feedback para persistência.</summary>
    void Add(AiSkillFeedback feedback);
}
