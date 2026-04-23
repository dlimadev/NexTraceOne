namespace NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

/// <summary>
/// Abstracção para leitura do estado de recolha de evidências pré-auditoria.
/// Por omissão satisfeita por <c>NullEvidenceCollectionStatusReader</c> (honest-null).
/// Wave BB.2 — GetEvidenceCollectionStatusReport.
/// </summary>
public interface IEvidenceCollectionStatusReader
{
    Task<IReadOnlyList<EvidenceControlEntry>> ListByTenantAsync(
        string tenantId, CancellationToken ct);

    Task<DateTimeOffset?> GetNextAuditDateAsync(string tenantId, CancellationToken ct);

    public sealed record EvidenceControlEntry(
        string ControlId,
        string ControlName,
        string Standard,
        bool IsCollected,
        bool IsAutoCollectable,
        DateTimeOffset? LastCollectedAt,
        string? EvidenceType);
}
