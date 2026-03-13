using NexTraceOne.DeveloperPortal.Domain.Entities;

namespace NexTraceOne.DeveloperPortal.Application.Abstractions;

/// <summary>
/// Repositório de sessões de playground do módulo DeveloperPortal.
/// Armazena histórico de execuções sandbox para auditoria e reutilização.
/// </summary>
public interface IPlaygroundSessionRepository
{
    Task<PlaygroundSession?> GetByIdAsync(PlaygroundSessionId id, CancellationToken ct = default);
    Task<IReadOnlyList<PlaygroundSession>> GetByUserAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<PlaygroundSession>> GetByApiAssetAsync(Guid apiAssetId, int page, int pageSize, CancellationToken ct = default);
    void Add(PlaygroundSession session);
}
