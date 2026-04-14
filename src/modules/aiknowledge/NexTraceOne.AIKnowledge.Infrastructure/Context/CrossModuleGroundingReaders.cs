using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Infrastructure.Contracts.Persistence;
using NexTraceOne.Catalog.Infrastructure.Graph.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence;
using NexTraceOne.Knowledge.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence;

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
            Description: svc.Description)).ToList();
    }
}

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

/// <summary>
/// Implementação do leitor de incidentes operacionais para grounding de IA.
/// Acesso somente-leitura ao IncidentDbContext.
/// </summary>
public sealed class IncidentGroundingReader(IncidentDbContext incidentDb) : IIncidentGroundingReader
{
    public async Task<IReadOnlyList<IncidentGroundingContext>> FindRecentIncidentsAsync(
        DateTimeOffset from,
        string? serviceId,
        string? environment,
        int maxResults,
        CancellationToken ct = default)
    {
        var query = incidentDb.Incidents
            .AsNoTracking()
            .Where(i => i.DetectedAt >= from);

        if (!string.IsNullOrWhiteSpace(serviceId))
            query = query.Where(i =>
                i.ServiceId == serviceId || i.ServiceName == serviceId ||
                i.ServiceName.Contains(serviceId));

        if (!string.IsNullOrWhiteSpace(environment))
            query = query.Where(i => i.Environment == environment);

        var incidents = await query
            .OrderByDescending(i => i.DetectedAt)
            .Take(maxResults)
            .ToListAsync(ct);

        return incidents.Select(i => new IncidentGroundingContext(
            IncidentId: i.Id.Value.ToString(),
            Title: i.Title,
            ServiceName: i.ServiceName,
            Severity: i.Severity.ToString(),
            Status: i.Status.ToString(),
            Environment: i.Environment,
            Description: i.Description,
            DetectedAt: i.DetectedAt)).ToList();
    }
}

/// <summary>
/// Implementação do leitor de documentos do Knowledge Hub para grounding de IA.
/// Acesso somente-leitura ao KnowledgeDbContext.
/// </summary>
public sealed class KnowledgeDocumentGroundingReader(KnowledgeDbContext knowledgeDb) : IKnowledgeDocumentGroundingReader
{
    public async Task<IReadOnlyList<KnowledgeDocumentGroundingContext>> SearchDocumentsAsync(
        string searchTerm,
        int maxResults,
        CancellationToken ct = default)
    {
        var term = searchTerm.Trim();
        if (term.Length == 0)
            return [];

        var docs = await knowledgeDb.KnowledgeDocuments
            .AsNoTracking()
            .Where(d =>
                d.Title.Contains(term) ||
                (d.Summary != null && d.Summary.Contains(term)) ||
                d.Content.Contains(term))
            .OrderByDescending(d => d.Title.Contains(term))
            .Take(maxResults)
            .ToListAsync(ct);

        return docs.Select(d => new KnowledgeDocumentGroundingContext(
            DocumentId: d.Id.Value.ToString(),
            Title: d.Title,
            Summary: d.Summary,
            Category: d.Category.ToString())).ToList();
    }
}

/// <summary>
/// Implementação do leitor de versões de contrato para grounding de IA.
/// Acesso somente-leitura ao ContractsDbContext do módulo Catalog.
/// </summary>
public sealed class ContractGroundingReader(ContractsDbContext contractsDb) : IContractGroundingReader
{
    public async Task<IReadOnlyList<ContractGroundingContext>> FindContractVersionsAsync(
        Guid? contractVersionId,
        Guid? apiAssetId,
        string? searchTerm,
        int maxResults,
        CancellationToken ct = default)
    {
        var query = contractsDb.ContractVersions.AsNoTracking();

        if (contractVersionId.HasValue)
            query = query.Where(cv => cv.Id == ContractVersionId.From(contractVersionId.Value));

        if (apiAssetId.HasValue)
            query = query.Where(cv => cv.ApiAssetId == apiAssetId.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(cv => cv.SemVer.Contains(searchTerm));

        var versions = await query
            .OrderByDescending(cv => cv.CreatedAt)
            .Take(maxResults)
            .ToListAsync(ct);

        return versions.Select(cv => new ContractGroundingContext(
            ContractVersionId: cv.Id.Value.ToString(),
            ApiAssetId: cv.ApiAssetId.ToString(),
            Version: cv.SemVer,
            Protocol: cv.Protocol.ToString(),
            LifecycleState: cv.LifecycleState.ToString(),
            IsLocked: cv.IsLocked,
            LockedAt: cv.LockedAt)).ToList();
    }
}
