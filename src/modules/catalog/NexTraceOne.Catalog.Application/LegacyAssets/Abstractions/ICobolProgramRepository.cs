using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;

/// <summary>
/// Repositório de programas COBOL do catálogo legacy.
/// </summary>
public interface ICobolProgramRepository
{
    Task<CobolProgram?> GetByIdAsync(CobolProgramId id, CancellationToken cancellationToken);
    Task<CobolProgram?> GetByNameAndSystemAsync(string name, MainframeSystemId systemId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CobolProgram>> ListBySystemAsync(MainframeSystemId systemId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CobolProgram>> SearchAsync(string searchTerm, CancellationToken cancellationToken);
    void Add(CobolProgram program);
}
