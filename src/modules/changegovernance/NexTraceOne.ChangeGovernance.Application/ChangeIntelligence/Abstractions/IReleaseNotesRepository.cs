using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>
/// Repositório para release notes geradas por IA.
/// </summary>
public interface IReleaseNotesRepository
{
    /// <summary>Adiciona novas release notes ao repositório.</summary>
    Task AddAsync(ReleaseNotes notes, CancellationToken cancellationToken);

    /// <summary>Obtém release notes pelo identificador da release associada.</summary>
    Task<ReleaseNotes?> GetByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken);

    /// <summary>Atualiza release notes existentes.</summary>
    void Update(ReleaseNotes notes);
}
