namespace NexTraceOne.Licensing.Contracts.DTOs;

/// <summary>
/// DTO público com o estado consolidado da licença.
/// </summary>
public sealed record LicenseStatusDto(
    Guid LicenseId,
    string LicenseKey,
    string CustomerName,
    bool IsActive,
    DateTimeOffset ExpiresAt,
    bool IsExpired,
    IReadOnlyList<CapabilityStatusDto> Capabilities,
    IReadOnlyList<UsageQuotaDto> UsageQuotas,
    int ActivationCount);
