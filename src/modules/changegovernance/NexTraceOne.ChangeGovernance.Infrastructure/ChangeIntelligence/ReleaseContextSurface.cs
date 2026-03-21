using Microsoft.EntityFrameworkCore;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence;

/// <summary>
/// Implementação stub da superfície de consulta contextual de releases para a IA.
/// Filtra releases por TenantId e, opcionalmente, por EnvironmentId e ServiceName.
/// Toda consulta é tenant-isolated por design.
/// </summary>
internal sealed class ReleaseContextSurface(ChangeIntelligenceDbContext db) : IReleaseContextSurface
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ReleaseContextEntry>> ListByContextAsync(
        Guid tenantId,
        Guid? environmentId,
        string? serviceName,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default)
    {
        var query = db.Releases
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId);

        if (environmentId.HasValue)
            query = query.Where(r => r.EnvironmentId == environmentId.Value);

        if (!string.IsNullOrWhiteSpace(serviceName))
            query = query.Where(r => r.ServiceName == serviceName);

        if (from.HasValue)
            query = query.Where(r => r.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(r => r.CreatedAt <= to.Value);

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReleaseContextEntry(
                r.Id.Value,
                r.ServiceName,
                r.Version,
                r.Environment,
                r.TenantId,
                r.EnvironmentId,
                r.Status.ToString(),
                r.ChangeScore,
                r.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReleaseContextEntry>> ListNonProductionReleasesAsync(
        Guid tenantId,
        DateTimeOffset since,
        CancellationToken cancellationToken = default)
    {
        return await db.Releases
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId
                && r.CreatedAt >= since
                // TODO (Fase 5): substituir por lookup de IsProductionLike via EnvironmentId
                // quando a entidade Environment estiver acessível neste módulo sem acoplamento direto.
                && !new[] { "production", "prod" }.Contains(r.Environment.ToLower()))
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReleaseContextEntry(
                r.Id.Value,
                r.ServiceName,
                r.Version,
                r.Environment,
                r.TenantId,
                r.EnvironmentId,
                r.Status.ToString(),
                r.ChangeScore,
                r.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
