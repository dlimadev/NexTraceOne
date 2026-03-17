using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de registos de inferência realizados por IA externa.
/// Suporta registo, consulta individual, listagem e filtragem por estado de revisão.
/// </summary>
public interface IAiExternalInferenceRecordRepository
{
    /// <summary>Adiciona um novo registo de inferência externa.</summary>
    Task AddAsync(AiExternalInferenceRecord entity, CancellationToken ct = default);

    /// <summary>Obtém um registo pelo identificador fortemente tipado.</summary>
    Task<AiExternalInferenceRecord?> GetByIdAsync(AiExternalInferenceRecordId id, CancellationToken ct = default);

    /// <summary>Lista todos os registos de inferência externa.</summary>
    Task<IReadOnlyList<AiExternalInferenceRecord>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Lista registos pendentes de revisão (PromotionStatus = Pending).</summary>
    Task<IReadOnlyList<AiExternalInferenceRecord>> GetPendingReviewAsync(CancellationToken ct = default);
}
