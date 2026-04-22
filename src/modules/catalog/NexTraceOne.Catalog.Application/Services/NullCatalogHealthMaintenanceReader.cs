using NexTraceOne.Catalog.Application.Services.Abstractions;

namespace NexTraceOne.Catalog.Application.Services;

/// <summary>
/// Implementação null (honest-null) de ICatalogHealthMaintenanceReader.
/// Retorna lista vazia — catálogo sem serviços para analisar.
/// Wave AM.3 — GetCatalogHealthMaintenanceReport.
/// </summary>
public sealed class NullCatalogHealthMaintenanceReader : ICatalogHealthMaintenanceReader
{
    public Task<IReadOnlyList<ICatalogHealthMaintenanceReader.ServiceMaintenanceEntry>> ListByTenantAsync(
        string tenantId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<ICatalogHealthMaintenanceReader.ServiceMaintenanceEntry>>([]);
}
