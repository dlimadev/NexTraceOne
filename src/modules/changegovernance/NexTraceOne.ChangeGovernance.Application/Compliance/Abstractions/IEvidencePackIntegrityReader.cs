namespace NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

/// <summary>
/// Abstracção para leitura de dados de integridade de Evidence Packs.
/// Por omissão satisfeita por <c>NullEvidencePackIntegrityReader</c> (honest-null).
/// Wave BC.2 — GetEvidencePackIntegrityReport.
/// </summary>
public interface IEvidencePackIntegrityReader
{
    Task<IReadOnlyList<EvidencePackEntry>> ListByTenantAsync(
        string tenantId, DateTimeOffset from, CancellationToken ct);

    public sealed record EvidencePackEntry(
        string EvidencePackId,
        string ReleaseId,
        string ServiceId,
        string ServiceName,
        bool IsProductionRelease,
        bool IsHashValid,
        bool IsComplete,
        bool IsConsistent,
        bool HasSignature,
        bool IsSignatureValid,
        DateTimeOffset CreatedAt,
        int EvidenceItemCount,
        int MissingEvidenceCount);
}
