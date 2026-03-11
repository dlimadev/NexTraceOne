using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Licensing.Application.Abstractions;
using ActivateLicenseFeature = NexTraceOne.Licensing.Application.Features.ActivateLicense.ActivateLicense;
using CheckCapabilityFeature = NexTraceOne.Licensing.Application.Features.CheckCapability.CheckCapability;
using TrackUsageMetricFeature = NexTraceOne.Licensing.Application.Features.TrackUsageMetric.TrackUsageMetric;
using NexTraceOne.Licensing.Domain.Entities;
using LicenseAggregate = NexTraceOne.Licensing.Domain.Entities.License;

namespace NexTraceOne.Licensing.Tests.Application.Features;

/// <summary>
/// Testes de handlers da camada Application do módulo Licensing.
/// </summary>
public sealed class LicensingApplicationTests
{
    [Fact]
    public async Task ActivateLicense_Should_ReturnActivationResponse_When_LicenseExists()
    {
        var now = new DateTimeOffset(2025, 01, 10, 10, 0, 0, TimeSpan.Zero);
        var license = CreateActiveLicense();
        var repository = Substitute.For<ILicenseRepository>();
        var hardwareFingerprintProvider = Substitute.For<IHardwareFingerprintProvider>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new ActivateLicenseFeature.Handler(repository, hardwareFingerprintProvider, dateTimeProvider);

        repository.GetByLicenseKeyAsync("LIC-001", Arg.Any<CancellationToken>()).Returns(license);
        hardwareFingerprintProvider.Generate().Returns("fingerprint-001");
        dateTimeProvider.UtcNow.Returns(now);

        var result = await sut.Handle(new ActivateLicenseFeature.Command("LIC-001", "system"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HardwareFingerprint.Should().Be("fingerprint-001");
    }

    [Fact]
    public async Task CheckCapability_Should_ReturnFailure_When_CapabilityIsMissing()
    {
        var now = new DateTimeOffset(2025, 01, 10, 10, 0, 0, TimeSpan.Zero);
        var license = CreateActiveLicense();
        _ = license.Activate("fingerprint-001", "system", now);
        var repository = Substitute.For<ILicenseRepository>();
        var hardwareFingerprintProvider = Substitute.For<IHardwareFingerprintProvider>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new CheckCapabilityFeature.Handler(repository, hardwareFingerprintProvider, dateTimeProvider);

        repository.GetByLicenseKeyAsync("LIC-001", Arg.Any<CancellationToken>()).Returns(license);
        hardwareFingerprintProvider.Generate().Returns("fingerprint-001");
        dateTimeProvider.UtcNow.Returns(now);

        var result = await sut.Handle(new CheckCapabilityFeature.Query("LIC-001", "missing:feature"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Licensing.Capability.NotLicensed");
    }

    [Fact]
    public async Task TrackUsageMetric_Should_ReturnFailure_When_QuotaIsExceeded()
    {
        var now = new DateTimeOffset(2025, 01, 10, 10, 0, 0, TimeSpan.Zero);
        var license = CreateActiveLicense();
        _ = license.Activate("fingerprint-001", "system", now);
        var repository = Substitute.For<ILicenseRepository>();
        var hardwareFingerprintProvider = Substitute.For<IHardwareFingerprintProvider>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var sut = new TrackUsageMetricFeature.Handler(repository, hardwareFingerprintProvider, dateTimeProvider);

        repository.GetByLicenseKeyAsync("LIC-001", Arg.Any<CancellationToken>()).Returns(license);
        hardwareFingerprintProvider.Generate().Returns("fingerprint-001");
        dateTimeProvider.UtcNow.Returns(now);

        var result = await sut.Handle(new TrackUsageMetricFeature.Command("LIC-001", "api.calls", 101), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Licensing.Quota.Exceeded");
    }

    private static LicenseAggregate CreateActiveLicense()
        => LicenseAggregate.Create(
            "LIC-001",
            "Acme Corp",
            new DateTimeOffset(2025, 01, 01, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 01, 01, 0, 0, 0, TimeSpan.Zero),
            2,
            [LicenseCapability.Create("catalog:read", "Catalog Read")],
            [UsageQuota.Create("api.calls", 100)]);
}
