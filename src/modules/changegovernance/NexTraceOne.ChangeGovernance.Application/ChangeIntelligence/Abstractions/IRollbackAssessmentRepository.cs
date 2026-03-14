using NexTraceOne.ChangeIntelligence.Domain.Entities;

namespace NexTraceOne.ChangeIntelligence.Application.Abstractions;

/// <summary>Contrato de repositório para avaliações de rollback.</summary>
public interface IRollbackAssessmentRepository
{
    /// <summary>Busca a avaliação de rollback de uma release.</summary>
    Task<RollbackAssessment?> GetByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken = default);

    /// <summary>Adiciona uma avaliação de rollback.</summary>
    void Add(RollbackAssessment assessment);
}
