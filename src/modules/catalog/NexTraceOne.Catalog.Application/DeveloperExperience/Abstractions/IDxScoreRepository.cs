using NexTraceOne.Catalog.Domain.DeveloperExperience.Entities;

namespace NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions;

/// <summary>Contrato de repositório para DxScore.</summary>
public interface IDxScoreRepository
{
    Task<DxScore?> GetByTeamAsync(string teamId, string period, CancellationToken ct = default);
    Task<IReadOnlyList<DxScore>> ListAsync(string? period = null, string? scoreLevel = null, CancellationToken ct = default);
    void Add(DxScore score);
}
