using Microsoft.EntityFrameworkCore;

using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Entities;
using NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Repositories;

/// <summary>
/// Repositório EF Core de ServiceRiskProfile.
/// Suporta obtenção do perfil mais recente e listagem ranqueada para Risk Center Report.
/// Wave F.2 — Risk Center.
/// </summary>
internal sealed class ServiceRiskProfileRepository(ChangeIntelligenceDbContext context)
    : IServiceRiskProfileRepository
{
    public Task<ServiceRiskProfile?> GetLatestByServiceAsync(
        string tenantId,
        Guid serviceAssetId,
        CancellationToken ct = default)
        => context.ServiceRiskProfiles
            .Where(p => p.TenantId == tenantId && p.ServiceAssetId == serviceAssetId && !p.IsDeleted)
            .OrderByDescending(p => p.ComputedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<ServiceRiskProfile>> ListByTenantRankedAsync(
        string tenantId,
        int maxResults = 50,
        CancellationToken ct = default)
    {
        // Um perfil por serviço (o mais recente), ranqueados por score descendente
        var latestPerService = await context.ServiceRiskProfiles
            .Where(p => p.TenantId == tenantId && !p.IsDeleted)
            .GroupBy(p => p.ServiceAssetId)
            .Select(g => g.OrderByDescending(p => p.ComputedAt).First())
            .OrderByDescending(p => p.OverallScore)
            .Take(maxResults)
            .ToListAsync(ct);

        return latestPerService;
    }

    public void Add(ServiceRiskProfile profile)
        => context.ServiceRiskProfiles.Add(profile);
}
