using NexTraceOne.BuildingBlocks.Domain.Results;

namespace NexTraceOne.Licensing.Domain.Errors;

/// <summary>
/// Catálogo centralizado de erros do módulo Licensing com códigos i18n.
/// </summary>
public static class LicensingErrors
{
    /// <summary>Licença não encontrada.</summary>
    public static Error LicenseNotFound(Guid licenseId)
        => Error.NotFound("Licensing.License.NotFound", "License '{0}' was not found.", licenseId);

    /// <summary>Licença não encontrada pela chave pública.</summary>
    public static Error LicenseKeyNotFound(string licenseKey)
        => Error.NotFound("Licensing.License.KeyNotFound", "License key '{0}' was not found.", licenseKey);

    /// <summary>Licença expirada.</summary>
    public static Error LicenseExpired(DateTimeOffset expiresAt)
        => Error.Business("Licensing.License.Expired", "License expired at '{0:O}'.", expiresAt);

    /// <summary>Licença desativada.</summary>
    public static Error LicenseInactive()
        => Error.Forbidden("Licensing.License.Inactive", "License is inactive.");

    /// <summary>Capability não licenciada.</summary>
    public static Error CapabilityNotLicensed(string capabilityCode)
        => Error.Forbidden("Licensing.Capability.NotLicensed", "Capability '{0}' is not licensed.", capabilityCode);

    /// <summary>Hardware não vinculado ainda.</summary>
    public static Error HardwareNotBound()
        => Error.Conflict("Licensing.Hardware.NotBound", "License is not yet bound to a hardware fingerprint.");

    /// <summary>Hardware divergente do vínculo registrado.</summary>
    public static Error HardwareMismatch()
        => Error.Security("Licensing.Hardware.Mismatch", "Hardware fingerprint does not match the bound license.");

    /// <summary>Limite de ativações atingido.</summary>
    public static Error ActivationLimitReached(int maxActivations)
        => Error.Conflict("Licensing.Activation.LimitReached", "License activation limit '{0}' has been reached.", maxActivations);

    /// <summary>Quota não encontrada.</summary>
    public static Error QuotaNotFound(string metricCode)
        => Error.NotFound("Licensing.Quota.NotFound", "Usage quota '{0}' was not found.", metricCode);

    /// <summary>Quota excedida.</summary>
    public static Error QuotaExceeded(string metricCode, long currentUsage, long limit)
        => Error.Business("Licensing.Quota.Exceeded", "Usage quota '{0}' exceeded the limit. Current '{1}', limit '{2}'.", metricCode, currentUsage, limit);
}
