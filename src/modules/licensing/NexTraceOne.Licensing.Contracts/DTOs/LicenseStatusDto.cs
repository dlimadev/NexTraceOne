namespace NexTraceOne.Licensing.Contracts.DTOs;

/// <summary>
/// DTO público com o estado consolidado da licença.
/// Inclui informações de tipo, edição, trial e grace period para
/// consumo por outros módulos e pelo frontend.
/// </summary>
public sealed record LicenseStatusDto(
    Guid LicenseId,
    string LicenseKey,
    string CustomerName,
    bool IsActive,
    DateTimeOffset ExpiresAt,
    bool IsExpired,
    bool IsInGracePeriod,
    int DaysRemaining,
    string LicenseType,
    string Edition,
    bool IsTrial,
    bool TrialConverted,
    int GracePeriodDays,
    IReadOnlyList<CapabilityStatusDto> Capabilities,
    IReadOnlyList<UsageQuotaDto> UsageQuotas,
    int ActivationCount);
