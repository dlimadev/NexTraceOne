using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Security.Licensing;
using NexTraceOne.Licensing.Application.Abstractions;
using NexTraceOne.Licensing.Contracts.DTOs;
using NexTraceOne.Licensing.Contracts.ServiceInterfaces;

namespace NexTraceOne.Licensing.Infrastructure.Services;

internal sealed class HardwareFingerprintProvider : IHardwareFingerprintProvider
{
    public string Generate() => HardwareFingerprint.Generate();
}

internal sealed class LicensingModuleService(
    ILicenseRepository licenseRepository,
    IHardwareFingerprintProvider hardwareFingerprintProvider,
    IDateTimeProvider dateTimeProvider) : ILicensingModule
{
    public async Task<LicenseStatusDto?> GetLicenseStatusAsync(string licenseKey, CancellationToken cancellationToken)
    {
        var license = await licenseRepository.GetByLicenseKeyAsync(licenseKey, cancellationToken);
        if (license is null)
        {
            return null;
        }

        var capabilities = license.Capabilities
            .Select(capability => new CapabilityStatusDto(capability.Code, capability.Name, capability.IsEnabled))
            .ToArray();

        var usageQuotas = license.UsageQuotas
            .Select(quota => new UsageQuotaDto(quota.MetricCode, quota.CurrentUsage, quota.Limit, quota.IsThresholdReached()))
            .ToArray();

        return new LicenseStatusDto(
            license.Id.Value,
            license.LicenseKey,
            license.CustomerName,
            license.IsActive,
            license.ExpiresAt,
            license.ExpiresAt <= dateTimeProvider.UtcNow,
            capabilities,
            usageQuotas,
            license.Activations.Count);
    }

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
}
