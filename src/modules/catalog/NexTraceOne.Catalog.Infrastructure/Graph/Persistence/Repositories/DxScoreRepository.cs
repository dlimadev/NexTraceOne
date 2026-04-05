using Microsoft.EntityFrameworkCore;
using NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions;
using NexTraceOne.Catalog.Domain.DeveloperExperience.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Repositories;

/// <summary>Repositório EF Core para DxScore.</summary>
internal sealed class DxScoreRepository(CatalogGraphDbContext context) : IDxScoreRepository
{
    public async Task<DxScore?> GetByTeamAsync(string teamId, string period, CancellationToken ct)
        => await context.DxScores
            .AsNoTracking()
            .Where(s => s.TeamId == teamId && s.Period == period)
            .OrderByDescending(s => s.ComputedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<DxScore>> ListAsync(string? period, string? scoreLevel, CancellationToken ct)
    {
        var query = context.DxScores.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(period)) query = query.Where(s => s.Period == period);
        if (!string.IsNullOrWhiteSpace(scoreLevel)) query = query.Where(s => s.ScoreLevel == scoreLevel);
        return await query.OrderByDescending(s => s.ComputedAt).ToListAsync(ct);
    }

    public void Add(DxScore score) => context.DxScores.Add(score);
}
