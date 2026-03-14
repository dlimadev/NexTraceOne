using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Licensing.Domain.Errors;

/// <summary>
/// Catálogo centralizado de erros do módulo Licensing com códigos i18n.
/// Cada código segue o padrão "Licensing.{Entidade}.{Condição}" para
/// mapeamento direto com chaves de tradução no frontend.
///
/// Decisão de design:
/// - Todos os códigos são estáveis e versionados para compatibilidade com i18n.
/// - Mensagens técnicas em inglês; frontend resolve a mensagem final via i18n.
/// - Argumentos dinâmicos via MessageArgs para interpolação segura.
/// </summary>
public static class LicensingErrors
{
    // ─── Licença ─────────────────────────────────────────────────────

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

    // ─── Capability ──────────────────────────────────────────────────

    /// <summary>Capability não licenciada.</summary>
    public static Error CapabilityNotLicensed(string capabilityCode)
        => Error.Forbidden("Licensing.Capability.NotLicensed", "Capability '{0}' is not licensed.", capabilityCode);

    // ─── Hardware ────────────────────────────────────────────────────

    /// <summary>Hardware não vinculado ainda.</summary>
    public static Error HardwareNotBound()
        => Error.Conflict("Licensing.Hardware.NotBound", "License is not yet bound to a hardware fingerprint.");

    /// <summary>Hardware divergente do vínculo registrado.</summary>
    public static Error HardwareMismatch()
        => Error.Security("Licensing.Hardware.Mismatch", "Hardware fingerprint does not match the bound license.");

    // ─── Ativação ────────────────────────────────────────────────────

    /// <summary>Limite de ativações atingido.</summary>
    public static Error ActivationLimitReached(int maxActivations)
        => Error.Conflict("Licensing.Activation.LimitReached", "License activation limit '{0}' has been reached.", maxActivations);

    // ─── Quota / Usage ───────────────────────────────────────────────

    /// <summary>Quota não encontrada.</summary>
    public static Error QuotaNotFound(string metricCode)
        => Error.NotFound("Licensing.Quota.NotFound", "Usage quota '{0}' was not found.", metricCode);

    /// <summary>Quota excedida.</summary>
    public static Error QuotaExceeded(string metricCode, long currentUsage, long limit)
        => Error.Business("Licensing.Quota.Exceeded", "Usage quota '{0}' exceeded the limit. Current '{1}', limit '{2}'.", metricCode, currentUsage, limit);

    // ─── Trial ───────────────────────────────────────────────────────

    /// <summary>Operação permitida apenas para licenças do tipo Trial.</summary>
    public static Error NotTrialLicense()
        => Error.Business("Licensing.Trial.NotTrial", "This operation is only allowed for trial licenses.");

    /// <summary>Trial já foi convertido para licença full.</summary>
    public static Error TrialAlreadyConverted()
        => Error.Conflict("Licensing.Trial.AlreadyConverted", "Trial has already been converted to a full license.");

    /// <summary>Limite de extensões de trial atingido.</summary>
    public static Error TrialExtensionLimitReached()
        => Error.Business("Licensing.Trial.ExtensionLimitReached", "Trial extension limit has been reached. Maximum 1 extension allowed.");

    /// <summary>Trial expirado sem conversão — acesso restrito.</summary>
    public static Error TrialExpired()
        => Error.Business("Licensing.Trial.Expired", "Trial period has expired. Convert to a full license to continue.");

    // ─── Vendor Operations ──────────────────────────────────────────

    /// <summary>Licença já revogada — operação não permitida.</summary>
    public static Error LicenseAlreadyRevoked()
        => Error.Conflict("Licensing.License.AlreadyRevoked", "License has already been revoked.");
}
