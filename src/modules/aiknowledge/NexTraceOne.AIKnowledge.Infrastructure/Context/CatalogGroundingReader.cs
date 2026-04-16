using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.Catalog.Infrastructure.Graph.Persistence;

namespace NexTraceOne.AIKnowledge.Infrastructure.Context;

/// <summary>
/// Implementação do leitor de contexto de serviços do Catálogo para grounding de IA.
/// Acesso somente-leitura ao CatalogGraphDbContext.
/// </summary>
public sealed class CatalogGroundingReader(CatalogGraphDbContext catalogDb) : ICatalogGroundingReader
{
    public async Task<IReadOnlyList<ServiceGroundingContext>> FindServicesAsync(
        string? serviceId,
        string searchTerm,
        int maxResults,
        CancellationToken ct = default)
    {
        var services = await catalogDb.ServiceAssets
            .AsNoTracking()
            .Where(s =>
                (serviceId != null && (s.Name == serviceId || s.DisplayName == serviceId)) ||
                s.Name.Contains(searchTerm) || s.DisplayName.Contains(searchTerm))
            .OrderBy(s => s.Name)
            .Take(maxResults)
            .ToListAsync(ct);

        return services.Select(svc => new ServiceGroundingContext(
            ServiceId: svc.Id.Value.ToString(),
            DisplayName: svc.DisplayName,
            TeamName: svc.TeamName,
            Domain: svc.Domain,
            Criticality: svc.Criticality.ToString(),
            Lifecycle: svc.LifecycleStatus.ToString(),
            ServiceType: svc.ServiceType.ToString(),
            Description: svc.Description,
            SubDomain: string.IsNullOrEmpty(svc.SubDomain) ? null : svc.SubDomain,
            Capability: string.IsNullOrEmpty(svc.Capability) ? null : svc.Capability,
            DataClassification: string.IsNullOrEmpty(svc.DataClassification) ? null : svc.DataClassification,
            RegulatoryScope: string.IsNullOrEmpty(svc.RegulatoryScope) ? null : svc.RegulatoryScope,
            SloTarget: string.IsNullOrEmpty(svc.SloTarget) ? null : svc.SloTarget,
            ProductOwner: string.IsNullOrEmpty(svc.ProductOwner) ? null : svc.ProductOwner,
            ContactChannel: string.IsNullOrEmpty(svc.ContactChannel) ? null : svc.ContactChannel)).ToList();
    }
}
