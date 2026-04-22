namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>Wave AQ.2 — GetSchemaQualityIndexReport.</summary>
public interface ISchemaQualityReader
{
    Task<IReadOnlyList<ContractSchemaEntry>> ListByTenantAsync(string tenantId, CancellationToken ct);
    Task<IReadOnlyList<SchemaQualitySnapshot>> GetMonthlySnapshotsAsync(string tenantId, int months, CancellationToken ct);

    public sealed record ContractSchemaEntry(
        string ContractId,
        string ContractName,
        string Protocol,
        string ServiceTier,
        int TotalFields,
        int FieldsWithDescription,
        int FieldsWithExamples,
        int OperationsWithErrorCodes,
        int TotalOperations,
        int FieldsWithConstraints,
        int EnumFieldsWith3PlusValues,
        int TotalEnumFields);

    public sealed record SchemaQualitySnapshot(
        DateTimeOffset SnapshotDate,
        double TenantSchemaHealthScore);
}
