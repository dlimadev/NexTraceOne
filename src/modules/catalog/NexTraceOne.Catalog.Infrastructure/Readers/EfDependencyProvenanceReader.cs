using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Infrastructure.Contracts.Persistence;

namespace NexTraceOne.Catalog.Infrastructure.Readers;

/// <summary>
/// Implementação real de IDependencyProvenanceReader.
/// Agrega componentes do SBOM mais recente de cada serviço do tenant, agrupados por
/// (Nome, Registo, Licença) para análise de proveniência, aprovação de registo e risco de SPOF.
/// Wave AO.2 — GetDependencyProvenanceReport.
/// </summary>
internal sealed class EfDependencyProvenanceReader(ContractsDbContext contractsDb) : IDependencyProvenanceReader
{
    public async Task<IReadOnlyList<ComponentProvenanceEntry>> ListComponentsByTenantAsync(
        string tenantId,
        IReadOnlyList<string> approvedRegistries,
        IReadOnlyList<string> highRiskLicenses,
        int spofThreshold,
        CancellationToken ct)
    {
        var sboms = await contractsDb.SbomRecords
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId)
            .ToListAsync(ct);

        if (sboms.Count == 0)
            return [];

        // SBOM mais recente por serviço
        var latestByService = sboms
            .GroupBy(s => s.ServiceId)
            .Select(g => g.OrderByDescending(s => s.RecordedAt).First())
            .ToList();

        // Agrega componentes: (Name, Registry, License) → lista de (version, serviceId, cveCount, severity)
        var componentMap = new Dictionary<(string Name, string Registry, string License),
            (List<string> Versions, HashSet<string> Services, int TotalCve, string TopSeverity)>();

        foreach (var sbom in latestByService)
        {
            foreach (var c in sbom.Components ?? [])
            {
                var key = (c.Name, c.Registry, c.License);

                if (!componentMap.TryGetValue(key, out var entry))
                {
                    componentMap[key] = entry = ([], [], 0, "None");
                }

                if (!entry.Versions.Contains(c.Version))
                    entry.Versions.Add(c.Version);

                entry.Services.Add(sbom.ServiceId);

                var updatedCve = entry.TotalCve + c.CveCount;
                var updatedSeverity = CompareSeverity(c.HighestCveSeverity, entry.TopSeverity) > 0
                    ? c.HighestCveSeverity
                    : entry.TopSeverity;

                componentMap[key] = (entry.Versions, entry.Services, updatedCve, updatedSeverity);
            }
        }

        var approved = approvedRegistries.Select(r => r.ToLowerInvariant()).ToHashSet();

        return componentMap
            .Select(kvp => new ComponentProvenanceEntry(
                ComponentName: kvp.Key.Name,
                VersionsInUse: kvp.Value.Versions,
                ServiceCount: kvp.Value.Services.Count,
                RegistryOrigin: kvp.Key.Registry,
                IsApprovedRegistry: approved.Count == 0
                    || approved.Contains(kvp.Key.Registry.ToLowerInvariant()),
                LicenseType: kvp.Key.License,
                TotalCveCount: kvp.Value.TotalCve,
                HighestSeverity: kvp.Value.TopSeverity))
            .OrderByDescending(e => e.ServiceCount)
            .ThenByDescending(e => e.TotalCveCount)
            .ToList();
    }

    private static int CompareSeverity(string a, string b)
    {
        static int Rank(string s) => s.ToUpperInvariant() switch
        {
            "CRITICAL" => 4,
            "HIGH" => 3,
            "MEDIUM" => 2,
            "LOW" => 1,
            _ => 0
        };
        return Rank(a).CompareTo(Rank(b));
    }
}
