using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de feedbacks de trajectória para Agent Lightning.
/// Suporta listagem de feedbacks pendentes de exportação para o trainer externo.
/// </summary>
public interface IAiAgentTrajectoryFeedbackRepository
{
    /// <summary>Obtém um feedback pelo identificador fortemente tipado.</summary>
    Task<AiAgentTrajectoryFeedback?> GetByIdAsync(AiAgentTrajectoryFeedbackId id, CancellationToken ct);

    /// <summary>Lista feedbacks ainda não exportados para treino, ordenados por data de submissão.</summary>
    Task<IReadOnlyList<AiAgentTrajectoryFeedback>> ListPendingExportAsync(int limit, CancellationToken ct);

    /// <summary>Verifica se já existe feedback para a execução especificada.</summary>
    Task<bool> ExistsByExecutionIdAsync(AiAgentExecutionId executionId, CancellationToken ct);

    /// <summary>Adiciona um novo feedback para persistência.</summary>
    void Add(AiAgentTrajectoryFeedback feedback);
}
