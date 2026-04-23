using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts;

/// <summary>Wave AQ.2 — honest-null ISchemaQualityReader.</summary>
public sealed class NullSchemaQualityReader : ISchemaQualityReader
{
    public Task<IReadOnlyList<ISchemaQualityReader.ContractSchemaEntry>> ListByTenantAsync(
        string tenantId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<ISchemaQualityReader.ContractSchemaEntry>>([]);

    public Task<IReadOnlyList<ISchemaQualityReader.SchemaQualitySnapshot>> GetMonthlySnapshotsAsync(
        string tenantId, int months, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<ISchemaQualityReader.SchemaQualitySnapshot>>([]);
}
