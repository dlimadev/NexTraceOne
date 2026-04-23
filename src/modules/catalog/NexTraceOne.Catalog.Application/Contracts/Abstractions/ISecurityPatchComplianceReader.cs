namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstracção para leitura de dados de compliance de patching de segurança.
/// Por omissão satisfeita por <c>NullSecurityPatchComplianceReader</c> (honest-null).
/// Wave AX.2 — GetSecurityPatchComplianceReport.
/// </summary>
public interface ISecurityPatchComplianceReader
{
    Task<IReadOnlyList<PatchComplianceEntry>> ListByTenantAsync(
        string tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct);

    /// <summary>Entrada de compliance de patching por serviço.</summary>
    public sealed record PatchComplianceEntry(
        string ServiceId,
        string ServiceName,
        string TeamName,
        string ServiceTier,
        IReadOnlyList<RemediatedCve> RemediatedCves,
        IReadOnlyList<ActiveCve> ActiveCves);

    /// <summary>CVE remediada dentro da janela de análise.</summary>
    public sealed record RemediatedCve(
        string CveId,
        string Severity,
        DateTimeOffset DiscoveredAt,
        DateTimeOffset RemediatedAt);

    /// <summary>CVE activa (ainda não remediada).</summary>
    public sealed record ActiveCve(
        string CveId,
        string Severity,
        DateTimeOffset DiscoveredAt);
}
