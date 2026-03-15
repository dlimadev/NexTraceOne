namespace NexTraceOne.Governance.Contracts;

/// <summary>
/// Eventos de integração publicados pelo módulo Governance.
/// Utilizado por outros módulos para reagir a mudanças de estado de governança.
/// </summary>
public static class IntegrationEvents
{
    /// <summary>Publicado quando um relatório de risco é gerado.</summary>
    public sealed record RiskReportGenerated(
        string ReportId,
        string Scope,
        DateTimeOffset GeneratedAt);

    /// <summary>Publicado quando gaps de compliance são detectados.</summary>
    public sealed record ComplianceGapsDetected(
        string ReportId,
        int GapCount,
        DateTimeOffset DetectedAt);
}
