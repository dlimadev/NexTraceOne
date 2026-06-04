using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Orchestration.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;
using NexTraceOne.AIKnowledge.Infrastructure.Persistence;

namespace NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Persistence.Repositories;

internal sealed class GeneratedTestArtifactRepository(AiHubDbContext context)
    : IGeneratedTestArtifactRepository
{
    public async Task AddAsync(GeneratedTestArtifact artifact, CancellationToken ct)
    {
        await context.TestArtifacts.AddAsync(artifact, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ArtifactSummaryData>> GetRecentByReleaseAsync(
        Guid releaseId,
        int maxCount,
        CancellationToken ct)
    {
        return await context.TestArtifacts
            .Where(a => a.ReleaseId == releaseId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(maxCount)
            .Select(a => new ArtifactSummaryData(
                a.ServiceName,
                a.TestFramework,
                a.Status.ToString(),
                a.Confidence))
            .ToListAsync(ct);
    }
}
