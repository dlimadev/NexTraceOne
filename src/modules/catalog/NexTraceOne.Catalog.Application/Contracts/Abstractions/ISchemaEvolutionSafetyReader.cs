namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>Wave AQ.3 — GetSchemaEvolutionSafetyReport.</summary>
public interface ISchemaEvolutionSafetyReader
{
    Task<IReadOnlyList<TeamSchemaEvolutionEntry>> ListByTenantAsync(
        string tenantId, int lookbackDays, CancellationToken ct);

    public sealed record TeamSchemaEvolutionEntry(
        string TeamId,
        string TeamName,
        int TotalSchemaChanges,
        int BreakingChanges,
        int BreakingChangesWithIncidentCorrelation,
        int ConsumerNotifiedBreakingChanges,
        IReadOnlyList<ProtocolBreakingEntry> ProtocolBreaking,
        IReadOnlyList<HighRiskChange> RecentHighRiskChanges);

    public sealed record ProtocolBreakingEntry(string Protocol, int Total, int Breaking);
    public sealed record HighRiskChange(string ContractId, string ContractName, DateTimeOffset ChangedAt);
}
