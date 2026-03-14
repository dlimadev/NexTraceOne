namespace NexTraceOne.Licensing.Domain.Enums;

/// <summary>
/// Status do consentimento de telemetria do tenant.
/// Respeita soberania de dados e regulamentações (LGPD/GDPR).
/// Auditável: toda mudança de consentimento gera evento.
/// </summary>
public enum TelemetryConsentStatus
{
    /// <summary>Consentimento ainda não solicitado ao tenant.</summary>
    NotRequested = 0,

    /// <summary>Tenant concedeu consentimento para coleta de telemetria.</summary>
    Granted = 1,

    /// <summary>Tenant recusou ou revogou o consentimento.</summary>
    Denied = 2,

    /// <summary>Consentimento parcial (apenas métricas agregadas, sem PII).</summary>
    Partial = 3
}
