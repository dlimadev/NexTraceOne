using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence;

namespace NexTraceOne.AIKnowledge.Infrastructure.Context;

/// <summary>
/// Implementação do leitor de releases do ChangeIntelligence para grounding de IA.
/// Acesso somente-leitura ao ChangeIntelligenceDbContext.
/// </summary>
public sealed class ChangeGroundingReader(ChangeIntelligenceDbContext changeDb) : IChangeGroundingReader
{
    public async Task<IReadOnlyList<ReleaseGroundingContext>> FindRecentReleasesAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string? serviceId,
        string? environment,
        Guid? tenantId,
        int maxResults,
        CancellationToken ct = default)
    {
        var query = changeDb.Releases
            .AsNoTracking()
            .Where(r => r.CreatedAt >= from && r.CreatedAt <= to);

        if (!string.IsNullOrWhiteSpace(serviceId))
            query = query.Where(r => r.ServiceName == serviceId || r.ServiceName.Contains(serviceId));

        if (!string.IsNullOrWhiteSpace(environment))
            query = query.Where(r => r.Environment == environment);

        if (tenantId.HasValue)
            query = query.Where(r => r.TenantId == tenantId);

        var releases = await query
            .OrderByDescending(r => r.CreatedAt)
            .Take(maxResults)
            .ToListAsync(ct);

        return releases.Select(r => new ReleaseGroundingContext(
            ReleaseId: r.Id.Value.ToString(),
            ServiceName: r.ServiceName,
            Version: r.Version,
            Environment: r.Environment,
            Status: r.Status.ToString(),
            ChangeLevel: r.ChangeLevel.ToString(),
            ChangeScore: r.ChangeScore,
            Description: r.Description,
            CreatedAt: r.CreatedAt)).ToList();
    }
}
