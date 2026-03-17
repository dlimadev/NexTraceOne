using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>Contrato de repositório para BlastRadiusReport.</summary>
public interface IBlastRadiusRepository
{
    /// <summary>Busca o relatório de blast radius de uma release.</summary>
    Task<BlastRadiusReport?> GetByReleaseIdAsync(ReleaseId releaseId, CancellationToken cancellationToken = default);

    /// <summary>Adiciona um novo relatório de blast radius.</summary>
    void Add(BlastRadiusReport report);
}
