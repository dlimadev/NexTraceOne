using Microsoft.EntityFrameworkCore;

using NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents;

/// <summary>
/// Implementação de leitura somente de releases do módulo ChangeIntelligence.
/// Consulta ChangeIntelligenceDbContext de forma independente, sem acoplar
/// o domínio de OperationalIntelligence ao domínio de ChangeGovernance.
/// </summary>
internal sealed class EfChangeIntelligenceReader(ChangeIntelligenceDbContext context)
    : IChangeIntelligenceReader
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ChangeReleaseDto>> GetReleasesInWindowAsync(
        string? environment,
        DateTimeOffset from,
        DateTimeOffset to,
        Guid? tenantId,
        CancellationToken cancellationToken)
    {
        var query = context.Releases
            .AsNoTracking()
            .Where(r => r.CreatedAt >= from && r.CreatedAt <= to);

        if (tenantId.HasValue)
            query = query.Where(r => r.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(environment))
            query = query.Where(r => r.Environment == environment);

        var releases = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return releases
            .Select(r => new ChangeReleaseDto(
                r.Id.Value,
                r.ApiAssetId,
                r.ServiceName,
                r.Environment,
                r.Description,
                r.CreatedAt,
                r.TenantId))
            .ToList();
    }
}
