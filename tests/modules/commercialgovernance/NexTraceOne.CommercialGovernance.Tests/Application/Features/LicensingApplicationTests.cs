using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Licensing.Application.Abstractions;
using NexTraceOne.Licensing.Domain.Entities;
using NexTraceOne.Licensing.Domain.Enums;
using LicenseAggregate = NexTraceOne.Licensing.Domain.Entities.License;
using ActivateLicenseFeature = NexTraceOne.Licensing.Application.Features.ActivateLicense.ActivateLicense;
using CheckCapabilityFeature = NexTraceOne.Licensing.Application.Features.CheckCapability.CheckCapability;
using TrackUsageMetricFeature = NexTraceOne.Licensing.Application.Features.TrackUsageMetric.TrackUsageMetric;
using VerifyLicenseOnStartupFeature = NexTraceOne.Licensing.Application.Features.VerifyLicenseOnStartup.VerifyLicenseOnStartup;
using GetLicenseStatusFeature = NexTraceOne.Licensing.Application.Features.GetLicenseStatus.GetLicenseStatus;
using GetLicenseHealthFeature = NexTraceOne.Licensing.Application.Features.GetLicenseHealth.GetLicenseHealth;
using StartTrialFeature = NexTraceOne.Licensing.Application.Features.StartTrial.StartTrial;
using ExtendTrialFeature = NexTraceOne.Licensing.Application.Features.ExtendTrial.ExtendTrial;
using ConvertTrialFeature = NexTraceOne.Licensing.Application.Features.ConvertTrial.ConvertTrial;

namespace NexTraceOne.Licensing.Tests.Application.Features;

/// <summary>
/// Testes de handlers da camada Application do módulo Licensing.
/// Cada handler é testado com NSubstitute para isolamento de repositórios e providers.
/// </summary>
public sealed class LicensingApplicationTests
{
    private static readonly DateTimeOffset Now = new(2025, 01, 10, 10, 0, 0, TimeSpan.Zero);
    private const string LicenseKey = "LIC-001";
    private const string Fingerprint = "fingerprint-001";

    // ─── ActivateLicense ─────────────────────────────────────────────

    [Fact]
    public async Task ActivateLicense_Should_ReturnActivationResponse_When_LicenseExists()
    {
        var (repo, hw, dt) = CreateMocks();
        var license = CreateActiveLicense();
        repo.GetByLicenseKeyAsync(LicenseKey, Arg.Any<CancellationToken>()).Returns(license);
        var sut = new ActivateLicenseFeature.Handler(repo, hw, dt);

        var result = await sut.Handle(new ActivateLicenseFeature.Command(LicenseKey, "system"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HardwareFingerprint.Should().Be(Fingerprint);
    }

    [Fact]
    public async Task ActivateLicense_Should_Fail_When_LicenseNotFound()
    {
        var (repo, hw, dt) = CreateMocks();
        repo.GetByLicenseKeyAsync("MISSING", Arg.Any<CancellationToken>()).Returns((LicenseAggregate)null!);
        var sut = new ActivateLicenseFeature.Handler(repo, hw, dt);

        var result = await sut.Handle(new ActivateLicenseFeature.Command("MISSING", "system"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Licensing.License.KeyNotFound");
    }

    // ─── CheckCapability ─────────────────────────────────────────────

    [Fact]
    public async Task CheckCapability_Should_ReturnFailure_When_CapabilityIsMissing()
    {
        var (repo, hw, dt) = CreateMocks();
        var license = CreateActivatedLicense();
        repo.GetByLicenseKeyAsync(LicenseKey, Arg.Any<CancellationToken>()).Returns(license);
        var sut = new CheckCapabilityFeature.Handler(repo, hw, dt);

        var result = await sut.Handle(new CheckCapabilityFeature.Query(LicenseKey, "missing:feature"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Licensing.Capability.NotLicensed");
    }

    [Fact]
    public async Task CheckCapability_Should_Succeed_When_CapabilityIsPresent()
    {
        var (repo, hw, dt) = CreateMocks();
        var license = CreateActivatedLicense();
        repo.GetByLicenseKeyAsync(LicenseKey, Arg.Any<CancellationToken>()).Returns(license);
        var sut = new CheckCapabilityFeature.Handler(repo, hw, dt);

        var result = await sut.Handle(new CheckCapabilityFeature.Query(LicenseKey, "catalog:read"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("catalog:read");
        result.Value.IsEnabled.Should().BeTrue();
    }

    // ─── TrackUsageMetric ────────────────────────────────────────────

    [Fact]
    public async Task TrackUsageMetric_Should_ReturnFailure_When_QuotaIsExceeded()
    {
        var (repo, hw, dt) = CreateMocks();
        var license = CreateActivatedLicense();
        repo.GetByLicenseKeyAsync(LicenseKey, Arg.Any<CancellationToken>()).Returns(license);
        var sut = new TrackUsageMetricFeature.Handler(repo, hw, dt);

        var result = await sut.Handle(new TrackUsageMetricFeature.Command(LicenseKey, "api.calls", 101), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Licensing.Quota.Exceeded");
    }

    [Fact]
    public async Task TrackUsageMetric_Should_Succeed_When_WithinQuota()
    {
        var (repo, hw, dt) = CreateMocks();
        var license = CreateActivatedLicense();
        repo.GetByLicenseKeyAsync(LicenseKey, Arg.Any<CancellationToken>()).Returns(license);
        var sut = new TrackUsageMetricFeature.Handler(repo, hw, dt);

        var result = await sut.Handle(new TrackUsageMetricFeature.Command(LicenseKey, "api.calls", 50), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentUsage.Should().Be(50);
        result.Value.MetricCode.Should().Be("api.calls");
    }

    // ─── VerifyLicenseOnStartup ──────────────────────────────────────

    [Fact]
    public async Task VerifyLicenseOnStartup_Should_Succeed_WithValidHardware()
    {
        var (repo, hw, dt) = CreateMocks();
        var license = CreateActivatedLicense();
        repo.GetByLicenseKeyAsync(LicenseKey, Arg.Any<CancellationToken>()).Returns(license);
        var sut = new VerifyLicenseOnStartupFeature.Handler(repo, hw, dt);

        var result = await sut.Handle(new VerifyLicenseOnStartupFeature.Query(LicenseKey), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.HardwareFingerprint.Should().Be(Fingerprint);
    }

    [Fact]
    public async Task VerifyLicenseOnStartup_Should_Fail_When_HardwareMismatch()
    {
        var (repo, _, dt) = CreateMocks();
        // Provider retorna fingerprint diferente do que foi usado na ativação
        var hw = Substitute.For<IHardwareFingerprintProvider>();
        hw.Generate().Returns("different-fingerprint");
        var license = CreateActivatedLicense();
        repo.GetByLicenseKeyAsync(LicenseKey, Arg.Any<CancellationToken>()).Returns(license);
        var sut = new VerifyLicenseOnStartupFeature.Handler(repo, hw, dt);

        var result = await sut.Handle(new VerifyLicenseOnStartupFeature.Query(LicenseKey), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Licensing.Hardware.Mismatch");
    }

    // ─── GetLicenseStatus ────────────────────────────────────────────

    [Fact]
    public async Task GetLicenseStatus_Should_ReturnCorrectStatus()
    {
        var (repo, _, dt) = CreateMocks();
        var license = CreateActiveLicense();
        repo.GetByLicenseKeyAsync(LicenseKey, Arg.Any<CancellationToken>()).Returns(license);
        var sut = new GetLicenseStatusFeature.Handler(repo, dt);

        var result = await sut.Handle(new GetLicenseStatusFeature.Query(LicenseKey), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeTrue();
        result.Value.LicenseKey.Should().Be(LicenseKey);
        result.Value.CustomerName.Should().Be("Acme Corp");
        result.Value.IsTrial.Should().BeFalse();
        result.Value.LicenseType.Should().Be("Standard");
        result.Value.CapabilityCount.Should().Be(1);
        result.Value.QuotaCount.Should().Be(1);
    }

    // ─── GetLicenseHealth ────────────────────────────────────────────

    [Fact]
    public async Task GetLicenseHealth_Should_ReturnHealthScore()
    {
        var (repo, _, dt) = CreateMocks();
        var license = CreateActiveLicense();
        repo.GetByLicenseKeyAsync(LicenseKey, Arg.Any<CancellationToken>()).Returns(license);
        var sut = new GetLicenseHealthFeature.Handler(repo, dt);

        var result = await sut.Handle(new GetLicenseHealthFeature.Query(LicenseKey), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HealthScore.Should().BeGreaterThanOrEqualTo(0.0m);
        result.Value.HealthScore.Should().BeLessThanOrEqualTo(1.0m);
        result.Value.IsActive.Should().BeTrue();
        result.Value.IsTrial.Should().BeFalse();
    }

    // ─── StartTrial ──────────────────────────────────────────────────

    [Fact]
    public async Task StartTrial_Should_CreateTrialLicense()
    {
        var repo = Substitute.For<ILicenseRepository>();
        var dt = Substitute.For<IDateTimeProvider>();
        dt.UtcNow.Returns(Now);
        var sut = new StartTrialFeature.Handler(repo, dt);

        var result = await sut.Handle(new StartTrialFeature.Command("Startup Inc", 30), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CustomerName.Should().Be("Startup Inc");
        result.Value.TrialDays.Should().Be(30);
        result.Value.LicenseKey.Should().StartWith("TRIAL-");
        result.Value.ExpiresAt.Should().Be(Now.AddDays(30));
        repo.Received(1).Add(Arg.Is<LicenseAggregate>(l => l.IsTrial));
    }

    // ─── ExtendTrial ─────────────────────────────────────────────────

    [Fact]
    public async Task ExtendTrial_Should_Succeed_ForValidTrial()
    {
        var (repo, _, dt) = CreateMocks();
        var trial = CreateTrialLicense();
        repo.GetByLicenseKeyAsync("TRIAL-001", Arg.Any<CancellationToken>()).Returns(trial);
        var sut = new ExtendTrialFeature.Handler(repo, dt);

        var result = await sut.Handle(new ExtendTrialFeature.Command("TRIAL-001", 15), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExtensionCount.Should().Be(1);
    }

    // ─── ConvertTrial ────────────────────────────────────────────────

    [Fact]
    public async Task ConvertTrial_Should_Succeed_ForValidTrial()
    {
        var (repo, _, dt) = CreateMocks();
        var trial = CreateTrialLicense();
        repo.GetByLicenseKeyAsync("TRIAL-001", Arg.Any<CancellationToken>()).Returns(trial);
        var sut = new ConvertTrialFeature.Handler(repo, dt);

        var result = await sut.Handle(
            new ConvertTrialFeature.Command("TRIAL-001", LicenseEdition.Enterprise, 365, 10, 15),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Edition.Should().Be("Enterprise");
        result.Value.MaxActivations.Should().Be(10);
        result.Value.GracePeriodDays.Should().Be(15);
    }

    // ─── Helpers ─────────────────────────────────────────────────────

    /// <summary>Cria mocks padrão de repositório, hardware provider e datetime provider.</summary>
    private static (ILicenseRepository repo, IHardwareFingerprintProvider hw, IDateTimeProvider dt) CreateMocks()
    {
        var repo = Substitute.For<ILicenseRepository>();
        var hw = Substitute.For<IHardwareFingerprintProvider>();
        var dt = Substitute.For<IDateTimeProvider>();
        hw.Generate().Returns(Fingerprint);
        dt.UtcNow.Returns(Now);
        return (repo, hw, dt);
    }

    /// <summary>Cria uma licença Standard ativa sem hardware binding.</summary>
    private static LicenseAggregate CreateActiveLicense()
        => LicenseAggregate.Create(
            LicenseKey,
            "Acme Corp",
            new DateTimeOffset(2025, 01, 01, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 01, 01, 0, 0, 0, TimeSpan.Zero),
            2,
            [LicenseCapability.Create("catalog:read", "Catalog Read")],
            [UsageQuota.Create("api.calls", 100)]);

    /// <summary>Cria uma licença Standard já ativada com hardware binding.</summary>
    private static LicenseAggregate CreateActivatedLicense()
    {
        var license = CreateActiveLicense();
        _ = license.Activate(Fingerprint, "system", Now);
        return license;
    }

    /// <summary>Cria uma licença Trial ativa.</summary>
    private static LicenseAggregate CreateTrialLicense()
        => LicenseAggregate.CreateTrial(
            "TRIAL-001",
            "Startup Inc",
            new DateTimeOffset(2025, 01, 01, 0, 0, 0, TimeSpan.Zero));
}
