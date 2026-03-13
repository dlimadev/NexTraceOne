using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Security.Licensing;
using NexTraceOne.Licensing.Application.Abstractions;
using NexTraceOne.Licensing.Contracts.DTOs;
using NexTraceOne.Licensing.Contracts.ServiceInterfaces;

namespace NexTraceOne.Licensing.Infrastructure.Services;

/// <summary>
/// Provedor de fingerprint de hardware para identificação da instalação.
/// Utiliza o componente de segurança do BuildingBlocks para geração
/// determinística baseada em características do host.
/// </summary>
internal sealed class HardwareFingerprintProvider : IHardwareFingerprintProvider
{
    public string Generate() => HardwareFingerprint.Generate();
}

/// <summary>
/// Implementação do contrato público ILicensingModule para consumo cross-module.
/// Converte entidades de domínio para DTOs públicos do Contracts layer.
///
/// Regra: Este serviço é a única forma de outros módulos consultarem
/// dados do Licensing — nunca via DbContext ou repositórios diretamente.
/// </summary>
internal sealed class LicensingModuleService(
    ILicenseRepository licenseRepository,
    IHardwareFingerprintProvider hardwareFingerprintProvider,
    IDateTimeProvider dateTimeProvider) : ILicensingModule
{
    /// <inheritdoc />
    public async Task<LicenseStatusDto?> GetLicenseStatusAsync(string licenseKey, CancellationToken cancellationToken)
    {
        var license = await licenseRepository.GetByLicenseKeyAsync(licenseKey, cancellationToken);
        if (license is null)
        {
            return null;
        }

        var now = dateTimeProvider.UtcNow;

        var capabilities = license.Capabilities
            .Select(capability => new CapabilityStatusDto(capability.Code, capability.Name, capability.IsEnabled))
            .ToArray();

        var usageQuotas = license.UsageQuotas
            .Select(quota => new UsageQuotaDto(
                quota.MetricCode,
                quota.CurrentUsage,
                quota.Limit,
                quota.IsThresholdReached(),
                quota.UsagePercentage,
                quota.GetWarningLevel().ToString(),
                quota.EnforcementLevel.ToString()))
            .ToArray();

        return new LicenseStatusDto(
            license.Id.Value,
            license.LicenseKey,
            license.CustomerName,
            license.IsActive,
            license.ExpiresAt,
            license.IsExpired(now),
            license.IsInGracePeriod(now),
            license.DaysUntilExpiration(now),
            license.Type.ToString(),
            license.Edition.ToString(),
            license.IsTrial,
            license.TrialConverted,
            license.GracePeriodDays,
            capabilities,
            usageQuotas,
            license.Activations.Count);
    }

    /// <inheritdoc />
    public async Task<bool> HasCapabilityAsync(string licenseKey, string capabilityCode, CancellationToken cancellationToken)
    {
        var license = await licenseRepository.GetByLicenseKeyAsync(licenseKey, cancellationToken);
        if (license is null)
        {
            return false;
        }

        var capabilityResult = license.CheckCapability(capabilityCode, dateTimeProvider.UtcNow);
        return capabilityResult.IsSuccess;
    }

    /// <inheritdoc />
    public async Task<bool> VerifyLicenseAsync(string licenseKey, CancellationToken cancellationToken)
    {
        var license = await licenseRepository.GetByLicenseKeyAsync(licenseKey, cancellationToken);
        if (license is null)
        {
            return false;
        }

        var verificationResult = license.VerifyAt(dateTimeProvider.UtcNow, hardwareFingerprintProvider.Generate());
        return verificationResult.IsSuccess;
    }

    /// <inheritdoc />
    public async Task<bool> IsTrialAsync(string licenseKey, CancellationToken cancellationToken)
    {
        var license = await licenseRepository.GetByLicenseKeyAsync(licenseKey, cancellationToken);
        return license?.IsTrial ?? false;
    }

    /// <inheritdoc />
    public async Task<decimal> GetHealthScoreAsync(string licenseKey, CancellationToken cancellationToken)
    {
        var license = await licenseRepository.GetByLicenseKeyAsync(licenseKey, cancellationToken);
        return license?.CalculateHealthScore(dateTimeProvider.UtcNow) ?? 0.0m;
    }
}
