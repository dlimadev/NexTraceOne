namespace NexTraceOne.Catalog.Application.Services.Abstractions;

/// <summary>
/// Abstracção para leitura de dados de qualidade de manutenção do catálogo.
/// Analisa ServiceAsset, ApiAsset, ServiceDependency, OwnershipRecord e Runbook.
/// Por omissão satisfeita por <c>NullCatalogHealthMaintenanceReader</c> (honest-null).
/// Wave AM.3 — GetCatalogHealthMaintenanceReport.
/// </summary>
public interface ICatalogHealthMaintenanceReader
{
    Task<IReadOnlyList<ServiceMaintenanceEntry>> ListByTenantAsync(
        string tenantId,
        CancellationToken ct);

    /// <summary>Entrada de qualidade de manutenção de um serviço.</summary>
    public sealed record ServiceMaintenanceEntry(
        string ServiceId,
        string ServiceName,
        string ServiceTier,
        int DescriptionWordCount,
        DateTimeOffset? LastOwnershipUpdate,
        bool HasApprovedContract,
        DateTimeOffset? LastDependencyUpdate,
        bool HasActiveRunbook,
        DateTimeOffset? LastMaintenanceActivity);
}
