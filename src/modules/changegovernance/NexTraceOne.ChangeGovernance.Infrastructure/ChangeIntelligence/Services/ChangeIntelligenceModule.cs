using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NexTraceOne.ChangeGovernance.Contracts.ChangeIntelligence.ServiceInterfaces;
using NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Services;

/// <summary>
/// Implementação do contrato público do módulo ChangeIntelligence.
/// Outros módulos consomem esta interface — nunca o DbContext ou repositórios diretamente.
/// </summary>
internal sealed class ChangeIntelligenceModule(
    ChangeIntelligenceDbContext context,
    ILogger<ChangeIntelligenceModule> logger) : IChangeIntelligenceModule
{
    /// <inheritdoc />
    public async Task<ReleaseDto?> GetReleaseAsync(Guid releaseId, CancellationToken cancellationToken)
    {
        logger.LogDebug("Fetching release {ReleaseId} for cross-module consumption", releaseId);

        var release = await context.Releases
            .AsNoTracking()
            .Where(r => r.Id.Value == releaseId)
            .Select(r => new ReleaseDto(
                r.Id.Value,
                r.ApiAssetId,
                r.ServiceName,
                r.Version,
                r.Environment,
                r.Status.ToString(),
                r.ChangeLevel.ToString(),
                r.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return release;
    }

    /// <inheritdoc />
    public async Task<decimal?> GetChangeScoreAsync(Guid releaseId, CancellationToken cancellationToken)
    {
        logger.LogDebug("Fetching change score for release {ReleaseId}", releaseId);

        var releaseTypedId = new Domain.ChangeIntelligence.Entities.ReleaseId(releaseId);

        return await context.ChangeScores
            .AsNoTracking()
            .Where(s => s.ReleaseId == releaseTypedId)
            .OrderByDescending(s => s.ComputedAt)
            .Select(s => (decimal?)s.Score)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<BlastRadiusDto?> GetBlastRadiusAsync(Guid releaseId, CancellationToken cancellationToken)
    {
        logger.LogDebug("Fetching blast radius report for release {ReleaseId}", releaseId);

        var releaseTypedId = new Domain.ChangeIntelligence.Entities.ReleaseId(releaseId);

        var report = await context.BlastRadiusReports
            .AsNoTracking()
            .Where(r => r.ReleaseId == releaseTypedId)
            .OrderByDescending(r => r.CalculatedAt)
            .Select(r => new BlastRadiusDto(
                r.Id.Value,
                r.ReleaseId.Value,
                r.TotalAffectedConsumers,
                r.DirectConsumers,
                r.TransitiveConsumers,
                r.CalculatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return report;
    }
}
