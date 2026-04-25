using Microsoft.Extensions.Caching.Memory;
using NexTraceOne.Catalog.Contracts.Graph.ServiceInterfaces;

namespace NexTraceOne.Integrations.Application.Services;

/// <summary>
/// Processor de enriquecimento que injeta atributos do Service Catalog em spans e logs ingeridos.
///
/// Para cada sinal cujo campo "service.name" (ou "service") seja encontrado no Catalog,
/// injeta os atributos:
///   - nextraceone.service.owner
///   - nextraceone.service.tier (via ICatalogGraphModule — futuro campo tier)
///   - nextraceone.service.contract_count (via ContractsByTeam)
///   - nextraceone.team.name
///
/// O enriquecimento é gracioso: não bloqueia a ingestão se o serviço não for encontrado.
/// Lookup cacheado por 5 minutos (IMemoryCache) para evitar round-trips ao Catalog.
/// Integrado no TenantPipelineEngine como stage Enrichment.
/// </summary>
public sealed class CatalogEnrichmentProcessor(
    ICatalogGraphModule catalogModule,
    IMemoryCache cache)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Enriquece o sinal JSON com metadados do Service Catalog.
    /// Retorna o JSON enriquecido ou o original se o serviço não for encontrado.
    /// </summary>
    public async Task<string> EnrichAsync(
        string signalJson,
        CancellationToken cancellationToken = default)
    {
        var serviceName = ExtractServiceName(signalJson);
        if (string.IsNullOrWhiteSpace(serviceName))
            return signalJson;

        var enrichmentData = await GetCachedEnrichmentAsync(serviceName, cancellationToken);
        if (enrichmentData is null)
            return signalJson;

        return ApplyEnrichment(signalJson, enrichmentData);
    }

    private async Task<ServiceEnrichmentData?> GetCachedEnrichmentAsync(string serviceName, CancellationToken ct)
    {
        var cacheKey = $"catalog-enrichment:{serviceName}";

        if (cache.TryGetValue(cacheKey, out ServiceEnrichmentData? cached))
            return cached;

        try
        {
            var exists = await catalogModule.ServiceAssetExistsAsync(serviceName, ct);
            if (!exists)
            {
                cache.Set(cacheKey, (ServiceEnrichmentData?)null, CacheTtl);
                return null;
            }

            var services = await catalogModule.ListAllServicesAsync(ct);
            var serviceInfo = services.FirstOrDefault(s =>
                string.Equals(s.ServiceName, serviceName, StringComparison.OrdinalIgnoreCase));

            if (serviceInfo is null)
            {
                cache.Set(cacheKey, (ServiceEnrichmentData?)null, CacheTtl);
                return null;
            }

            var contracts = await catalogModule.ListContractsByTeamAsync(serviceInfo.TeamName, ct);
            var contractCount = contracts.Count;

            var data = new ServiceEnrichmentData(
                ServiceName: serviceName,
                TeamName: serviceInfo.TeamName,
                ContractCount: contractCount);

            cache.Set(cacheKey, data, CacheTtl);
            return data;
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractServiceName(string signalJson)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(signalJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("service.name", out var sn))
                return sn.GetString();
            if (root.TryGetProperty("service", out var s))
                return s.GetString();
            if (root.TryGetProperty("resource", out var res) &&
                res.TryGetProperty("service.name", out var rsn))
                return rsn.GetString();

            return null;
        }
        catch { return null; }
    }

    private static string ApplyEnrichment(string signalJson, ServiceEnrichmentData data)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(signalJson);
            var obj = System.Text.Json.Nodes.JsonObject.Create(doc.RootElement)!;

            obj["nextraceone.service.owner"] = data.TeamName;
            obj["nextraceone.service.contract_count"] = data.ContractCount;
            obj["nextraceone.team.name"] = data.TeamName;

            return obj.ToJsonString();
        }
        catch { return signalJson; }
    }

    private sealed record ServiceEnrichmentData(
        string ServiceName,
        string TeamName,
        int ContractCount);
}
