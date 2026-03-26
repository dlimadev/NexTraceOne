using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>Contrato de repositório para reviews pós-release.</summary>
public interface IPostReleaseReviewRepository
{
    /// <summary>Busca a review de uma release.</summary>
    Task<PostReleaseReview?> GetByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken = default);

    /// <summary>Adiciona uma review pós-release.</summary>
    void Add(PostReleaseReview review);

    /// <summary>Atualiza uma review pós-release existente.</summary>
    void Update(PostReleaseReview review);
}
