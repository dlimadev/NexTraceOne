namespace NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

/// <summary>
/// Abstracção para leitura de dados de correlação de incidentes de segurança com CVEs.
/// Por omissão satisfeita por <c>NullSecurityIncidentCorrelationReader</c> (honest-null).
/// Wave AX.3 — GetSecurityIncidentCorrelationReport.
/// </summary>
public interface ISecurityIncidentCorrelationReader
{
    Task<IReadOnlyList<SecurityIncidentEntry>> ListSecurityIncidentsByTenantAsync(
        string tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct);

    /// <summary>Entrada de incidente de segurança com sinais de correlação.</summary>
    public sealed record SecurityIncidentEntry(
        Guid IncidentId,
        string ServiceId,
        string ServiceName,
        DateTimeOffset OccurredAt,
        int ActiveCveCountAtTime,
        bool CriticalCvePresentAtTime,
        bool VulnerableComponentIntroducedRecently,
        IReadOnlyList<string> IntroducedVulnerableComponents);
}
