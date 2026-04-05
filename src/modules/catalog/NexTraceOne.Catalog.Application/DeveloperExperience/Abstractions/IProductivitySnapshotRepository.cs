using NexTraceOne.Catalog.Domain.DeveloperExperience.Entities;

namespace NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions;

/// <summary>Contrato de repositório para ProductivitySnapshot.</summary>
public interface IProductivitySnapshotRepository
{
    Task<IReadOnlyList<ProductivitySnapshot>> ListByTeamAsync(string teamId, DateTimeOffset? from = null, CancellationToken ct = default);
    void Add(ProductivitySnapshot snapshot);
}
