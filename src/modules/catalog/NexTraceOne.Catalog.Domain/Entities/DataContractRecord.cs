namespace NexTraceOne.Catalog.Domain.Entities;

/// <summary>
/// Entidade de domínio que representa um data contract registado para um serviço.
/// Wave AQ.1 — DataContractRecord.
/// </summary>
public sealed class DataContractRecord
{
    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public string ServiceId { get; private set; } = string.Empty;
    public string DatasetName { get; private set; } = string.Empty;
    public string ContractVersion { get; private set; } = string.Empty;
    public int? FreshnessRequirementHours { get; private set; }
    public string? FieldDefinitionsJson { get; private set; }
    public string? OwnerTeamId { get; private set; }
    public string Status { get; private set; } = "Active";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private DataContractRecord() { }

    public static DataContractRecord Create(
        string tenantId, string serviceId, string datasetName, string contractVersion,
        int? freshnessRequirementHours, string? fieldDefinitionsJson, string? ownerTeamId,
        DateTimeOffset now)
    {
        return new DataContractRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ServiceId = serviceId,
            DatasetName = datasetName,
            ContractVersion = contractVersion,
            FreshnessRequirementHours = freshnessRequirementHours,
            FieldDefinitionsJson = fieldDefinitionsJson,
            OwnerTeamId = ownerTeamId,
            Status = "Active",
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
