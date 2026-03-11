using FluentAssertions;
using NexTraceOne.Licensing.Domain.Entities;
using LicenseAggregate = NexTraceOne.Licensing.Domain.Entities.License;

namespace NexTraceOne.Licensing.Tests.Domain.Entities;

/// <summary>
/// Testes de domínio para o aggregate License.
/// </summary>
public sealed class LicenseTests
{
    [Fact]
    public void Create_Should_CreateActiveLicense_When_InputIsValid()
    {
        var license = LicenseAggregate.Create(
            "LIC-001",
            "Acme Corp",
            new DateTimeOffset(2025, 01, 01, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 01, 01, 0, 0, 0, TimeSpan.Zero),
            1,
            [LicenseCapability.Create("catalog:read", "Catalog Read")],
            [UsageQuota.Create("api.calls", 100)]);

        license.IsActive.Should().BeTrue();
        license.Capabilities.Should().ContainSingle();
        license.UsageQuotas.Should().ContainSingle();
    }

    [Fact]
    public void Activate_Should_BindHardware_When_FirstActivationOccurs()
    {
        var license = CreateValidLicense();
        var now = new DateTimeOffset(2025, 01, 10, 10, 0, 0, TimeSpan.Zero);

        var result = license.Activate("fingerprint-001", "system", now);

        result.IsSuccess.Should().BeTrue();
        license.HardwareBinding.Should().NotBeNull();
        license.HardwareBinding!.Fingerprint.Should().Be("fingerprint-001");
    }

    [Fact]
    public void VerifyAt_Should_ReturnFailure_When_HardwareDoesNotMatch()
    {
        var license = CreateValidLicense();
        var now = new DateTimeOffset(2025, 01, 10, 10, 0, 0, TimeSpan.Zero);
        _ = license.Activate("fingerprint-001", "system", now);

        var result = license.VerifyAt(now, "fingerprint-999");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Licensing.Hardware.Mismatch");
    }

    [Fact]
    public void CheckCapability_Should_ReturnFailure_When_CapabilityIsMissing()
    {
        var license = CreateValidLicense();
        var now = new DateTimeOffset(2025, 01, 10, 10, 0, 0, TimeSpan.Zero);
        var result = license.CheckCapability("missing:feature", now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Licensing.Capability.NotLicensed");
    }

    [Fact]
    public void TrackUsage_Should_ReturnFailure_When_QuotaIsExceeded()
    {
        var license = CreateValidLicense();
        var now = new DateTimeOffset(2025, 01, 10, 10, 0, 0, TimeSpan.Zero);

        var result = license.TrackUsage("api.calls", 101, now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Licensing.Quota.Exceeded");
    }

    private static LicenseAggregate CreateValidLicense()
        => LicenseAggregate.Create(
            "LIC-001",
            "Acme Corp",
            new DateTimeOffset(2025, 01, 01, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 01, 01, 0, 0, 0, TimeSpan.Zero),
            2,
            [LicenseCapability.Create("catalog:read", "Catalog Read")],
            [UsageQuota.Create("api.calls", 100)]);
}
