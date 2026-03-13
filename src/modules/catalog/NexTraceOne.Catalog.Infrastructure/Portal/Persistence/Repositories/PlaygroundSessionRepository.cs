using Microsoft.EntityFrameworkCore;
using NexTraceOne.DeveloperPortal.Application.Abstractions;
using NexTraceOne.DeveloperPortal.Domain.Entities;

namespace NexTraceOne.DeveloperPortal.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de sessões de playground, implementando persistência via EF Core.
/// Suporta consultas paginadas por utilizador e por API para histórico de execuções.
/// </summary>
internal sealed class PlaygroundSessionRepository(DeveloperPortalDbContext context) : IPlaygroundSessionRepository
{
    /// <summary>Busca sessão por identificador único.</summary>
    public async Task<PlaygroundSession?> GetByIdAsync(PlaygroundSessionId id, CancellationToken ct = default)
        => await context.PlaygroundSessions.SingleOrDefaultAsync(s => s.Id == id, ct);

    /// <summary>Lista sessões de um utilizador com paginação, ordenadas da mais recente para a mais antiga.</summary>
    public async Task<IReadOnlyList<PlaygroundSession>> GetByUserAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
        => await context.PlaygroundSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.ExecutedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    /// <summary>Lista sessões de uma API com paginação para análise de utilização.</summary>
    public async Task<IReadOnlyList<PlaygroundSession>> GetByApiAssetAsync(Guid apiAssetId, int page, int pageSize, CancellationToken ct = default)
        => await context.PlaygroundSessions
            .Where(s => s.ApiAssetId == apiAssetId)
            .OrderByDescending(s => s.ExecutedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    /// <summary>Adiciona nova sessão ao contexto.</summary>
    public void Add(PlaygroundSession session)
        => context.PlaygroundSessions.Add(session);
}
