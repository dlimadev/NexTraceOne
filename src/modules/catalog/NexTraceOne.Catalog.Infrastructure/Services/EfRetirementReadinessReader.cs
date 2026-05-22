using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.Services.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Infrastructure.Contracts.Persistence;
using NexTraceOne.Catalog.Infrastructure.Graph.Persistence;

namespace NexTraceOne.Catalog.Infrastructure.Services;

/// <summary>
/// Implementação EF Core de <see cref="IRetirementReadinessReader"/>.
///
/// Agrega dados de prontidão para retirada de um serviço a partir de:
/// - <see cref="CatalogGraphDbContext"/>: ServiceAssets, ApiAssets, ConsumerRelationships
/// - <see cref="ContractsDbContext"/>: ContractVersions (por ApiAssetId)
///
/// Wave AF.2 — GetServiceRetirementReadinessReport.
/// </summary>
internal sealed class EfRetirementReadinessReader(
    CatalogGraphDbContext graphDbContext,
    ContractsDbContext contractsDbContext) : IRetirementReadinessReader
{
    public async Task<RetirementReadinessData?> GetByServiceAsync(
        string tenantId, string serviceId, CancellationToken ct)
    {
        if (!Guid.TryParse(serviceId, out var serviceGuid))
            return null;

        var tenantGuid = Guid.TryParse(tenantId, out var tg) ? tg : (Guid?)null;

        var service = await graphDbContext.ServiceAssets
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == ServiceAssetId.From(serviceGuid)
                && (tenantGuid == null || s.TenantId == tenantGuid.Value), ct);

        if (service is null)
            return null;

        // ApiAssets do serviço (usando shadow FK OwnerServiceId)
        var apiAssets = await graphDbContext.ApiAssets
            .AsNoTracking()
            .Include(a => a.ConsumerRelationships)
            .Where(a => EF.Property<Guid>(a, "OwnerServiceId") == serviceGuid)
            .ToListAsync(ct);

        var apiAssetIds = apiAssets.Select(a => a.Id.Value).ToHashSet();

        // Consumidores: consumer relationships de todos os ApiAssets
        var consumerRelationships = apiAssets.SelectMany(a => a.ConsumerRelationships).ToList();
        var totalConsumers = consumerRelationships.Count;

        var uniqueConsumerNames = consumerRelationships
            .Select(c => c.ConsumerName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var totalConsumerTeams = uniqueConsumerNames.Count;

        // Contratos: ContractVersions associados a estes ApiAssets
        var contractVersions = await contractsDbContext.ContractVersions
            .AsNoTracking()
            .Where(cv => apiAssetIds.Contains(cv.ApiAssetId))
            .ToListAsync(ct);

        var totalContracts = contractVersions.Count;
        var deprecatedContracts = contractVersions
            .Count(cv => cv.LifecycleState is
                ContractLifecycleState.Deprecated or
                ContractLifecycleState.Sunset or
                ContractLifecycleState.Retired);

        // Consumidores não migrados: heurística — consumers com alta confiança ainda activos
        var unmigratedConsumers = consumerRelationships
            .Where(c => c.ConfidenceScore >= 0.5m)
            .Take(20)
            .Select(c => new BlockerConsumerInfo(
                ConsumerServiceName: c.ConsumerName,
                ConsumerTeamName: "Unknown",
                ConsumerTier: "Standard",
                IsNotified: false))
            .ToList();

        return new RetirementReadinessData(
            ServiceId: serviceId,
            ServiceName: service.DisplayName.Length > 0 ? service.DisplayName : service.Name,
            TeamName: service.TeamName.Length > 0 ? service.TeamName : "Unknown",
            CurrentLifecycleState: service.LifecycleStatus.ToString(),
            TotalConsumers: totalConsumers,
            MigratedConsumers: 0,
            TotalContracts: totalContracts,
            DeprecatedContracts: deprecatedContracts,
            HasApprovedDecommissionRunbook: false,
            TotalConsumerTeams: totalConsumerTeams,
            NotifiedConsumerTeams: 0,
            UnmigratedConsumers: unmigratedConsumers);
    }
}
