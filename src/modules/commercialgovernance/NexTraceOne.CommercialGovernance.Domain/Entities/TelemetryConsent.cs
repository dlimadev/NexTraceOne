using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;
using NexTraceOne.Licensing.Domain.Enums;

namespace NexTraceOne.Licensing.Domain.Entities;

/// <summary>
/// Registra o consentimento de telemetria de um tenant.
/// Respeita soberania de dados (LGPD/GDPR) e permite que cada tenant
/// controle individualmente a coleta de dados de uso.
///
/// Decisão de design:
/// - Entidade separada de License para permitir gestão independente.
/// - Todo estado é auditável: mudanças de consentimento geram eventos.
/// - Suporta consentimento parcial (métricas agregadas sem PII).
/// - Associada à licença via LicenseId para rastreabilidade.
/// - Timestamps recebidos por parâmetro (nunca DateTimeOffset.UtcNow)
///   para respeitar a regra de IDateTimeProvider na camada de Application.
/// </summary>
public sealed class TelemetryConsent : Entity<TelemetryConsentId>
{
    private TelemetryConsent() { }

    /// <summary>Identificador da licença associada.</summary>
    public LicenseId LicenseId { get; private set; } = null!;

    /// <summary>Status atual do consentimento.</summary>
    public TelemetryConsentStatus Status { get; private set; }

    /// <summary>Data/hora UTC da última alteração de consentimento.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Identificador de quem alterou o consentimento.</summary>
    public string UpdatedBy { get; private set; } = string.Empty;

    /// <summary>Justificativa ou motivo da alteração.</summary>
    public string? Reason { get; private set; }

    /// <summary>Indica se a coleta de métricas de uso está permitida.</summary>
    public bool AllowUsageMetrics { get; private set; }

    /// <summary>Indica se a coleta de dados de performance está permitida.</summary>
    public bool AllowPerformanceData { get; private set; }

    /// <summary>Indica se a coleta de dados de erro/diagnóstico está permitida.</summary>
    public bool AllowErrorDiagnostics { get; private set; }

    /// <summary>
    /// Cria um novo registro de consentimento de telemetria com status inicial.
    /// O timestamp é recebido por parâmetro para que a camada de Application
    /// forneça o valor via IDateTimeProvider, mantendo o domínio puro.
    /// </summary>
    public static TelemetryConsent Create(
        LicenseId licenseId,
        TelemetryConsentStatus status,
        string updatedBy,
        DateTimeOffset updatedAt,
        string? reason = null,
        bool allowUsageMetrics = false,
        bool allowPerformanceData = false,
        bool allowErrorDiagnostics = false)
        => new()
        {
            Id = TelemetryConsentId.New(),
            LicenseId = Guard.Against.Null(licenseId),
            Status = status,
            UpdatedAt = updatedAt,
            UpdatedBy = Guard.Against.NullOrWhiteSpace(updatedBy),
            Reason = reason,
            AllowUsageMetrics = allowUsageMetrics,
            AllowPerformanceData = allowPerformanceData,
            AllowErrorDiagnostics = allowErrorDiagnostics
        };

    /// <summary>Concede consentimento total para coleta de telemetria.</summary>
    public void Grant(string updatedBy, DateTimeOffset updatedAt, string? reason = null)
    {
        Status = TelemetryConsentStatus.Granted;
        AllowUsageMetrics = true;
        AllowPerformanceData = true;
        AllowErrorDiagnostics = true;
        UpdatedBy = Guard.Against.NullOrWhiteSpace(updatedBy);
        Reason = reason;
        UpdatedAt = updatedAt;
    }

    /// <summary>Revoga todo consentimento de telemetria.</summary>
    public void Deny(string updatedBy, DateTimeOffset updatedAt, string? reason = null)
    {
        Status = TelemetryConsentStatus.Denied;
        AllowUsageMetrics = false;
        AllowPerformanceData = false;
        AllowErrorDiagnostics = false;
        UpdatedBy = Guard.Against.NullOrWhiteSpace(updatedBy);
        Reason = reason;
        UpdatedAt = updatedAt;
    }

    /// <summary>Concede consentimento parcial (apenas métricas agregadas, sem PII).</summary>
    public void GrantPartial(
        string updatedBy,
        DateTimeOffset updatedAt,
        bool allowUsageMetrics,
        bool allowPerformanceData,
        bool allowErrorDiagnostics,
        string? reason = null)
    {
        Status = TelemetryConsentStatus.Partial;
        AllowUsageMetrics = allowUsageMetrics;
        AllowPerformanceData = allowPerformanceData;
        AllowErrorDiagnostics = allowErrorDiagnostics;
        UpdatedBy = Guard.Against.NullOrWhiteSpace(updatedBy);
        Reason = reason;
        UpdatedAt = updatedAt;
    }
}

/// <summary>Identificador fortemente tipado de TelemetryConsent.</summary>
public sealed record TelemetryConsentId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static TelemetryConsentId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static TelemetryConsentId From(Guid id) => new(id);
}
