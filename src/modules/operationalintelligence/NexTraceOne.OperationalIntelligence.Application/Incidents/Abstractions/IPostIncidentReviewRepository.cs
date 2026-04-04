using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;

/// <summary>
/// Repositório de Post-Incident Reviews (PIR) — contrato do domínio para persistência
/// e consulta de revisões pós-incidente.
/// </summary>
public interface IPostIncidentReviewRepository
{
    /// <summary>Obtém o PIR associado a um incidente, ou null se não existir.</summary>
    Task<PostIncidentReview?> GetByIncidentIdAsync(Guid incidentId, CancellationToken cancellationToken = default);

    /// <summary>Obtém um PIR pelo seu identificador.</summary>
    Task<PostIncidentReview?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Persiste um novo PIR.</summary>
    Task AddAsync(PostIncidentReview review, CancellationToken cancellationToken = default);

    /// <summary>Persiste alterações ao PIR existente.</summary>
    Task UpdateAsync(PostIncidentReview review, CancellationToken cancellationToken = default);
}
