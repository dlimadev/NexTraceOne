using FluentAssertions;
using NexTraceOne.Licensing.Domain.Entities;
using NexTraceOne.Licensing.Domain.Enums;
using LicenseAggregate = NexTraceOne.Licensing.Domain.Entities.License;

namespace NexTraceOne.Licensing.Tests.Domain.Entities;

/// <summary>
/// Testes de domínio para o aggregate License.
/// Cobre ciclo de vida, trial, capabilities, quotas, hardware binding e health score.
/// </summary>
public sealed class LicenseTests
{
    private static readonly DateTimeOffset BaseDate = new(2025, 01, 01, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset MidDate = new(2025, 01, 10, 10, 0, 0, TimeSpan.Zero);

    // ─── Create ──────────────────────────────────────────────────────

    [Fact]
    public void Create_Should_CreateActiveLicense_When_InputIsValid()
    {
        var license = LicenseAggregate.Create(
            "LIC-001",
            "Acme Corp",
            BaseDate,
            BaseDate.AddYears(1),
            1,
            [LicenseCapability.Create("catalog:read", "Catalog Read")],
            [UsageQuota.Create("api.calls", 100)]);

        license.IsActive.Should().BeTrue();
        license.Capabilities.Should().ContainSingle();
        license.UsageQuotas.Should().ContainSingle();
    }

    [Fact]
    public void Create_Should_SetCorrectTypeAndEdition()
    {
        var license = LicenseAggregate.Create(
            "LIC-ENT",
            "BigCorp",
            BaseDate,
            BaseDate.AddYears(1),
            5,
            type: LicenseType.Enterprise,
            edition: LicenseEdition.Enterprise,
            gracePeriodDays: 30);

        license.Type.Should().Be(LicenseType.Enterprise);
        license.Edition.Should().Be(LicenseEdition.Enterprise);
        license.GracePeriodDays.Should().Be(30);
        license.MaxActivations.Should().Be(5);
    }

    [Fact]
    public void Create_Should_Throw_When_MaxActivationsIsZero()
    {
        var act = () => LicenseAggregate.Create(
            "LIC-001",
            "Acme Corp",
            BaseDate,
            BaseDate.AddYears(1),
            maxActivations: 0);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("maxActivations");
    }

    // ─── Activate ────────────────────────────────────────────────────

    [Fact]
    public void Activate_Should_BindHardware_When_FirstActivationOccurs()
    {
        var license = CreateValidLicense();

        var result = license.Activate("fingerprint-001", "system", MidDate);

        result.IsSuccess.Should().BeTrue();
        license.HardwareBinding.Should().NotBeNull();
        license.HardwareBinding!.Fingerprint.Should().Be("fingerprint-001");
    }

    // ─── VerifyAt ────────────────────────────────────────────────────

    [Fact]
    public void VerifyAt_Should_ReturnFailure_When_HardwareDoesNotMatch()
    {
        var license = CreateValidLicense();
        _ = license.Activate("fingerprint-001", "system", MidDate);

        var result = license.VerifyAt(MidDate, "fingerprint-999");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Licensing.Hardware.Mismatch");
    }

    // ─── CheckCapability ─────────────────────────────────────────────

    [Fact]
    public void CheckCapability_Should_ReturnFailure_When_CapabilityIsMissing()
    {
        var license = CreateValidLicense();
        var result = license.CheckCapability("missing:feature", MidDate);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Licensing.Capability.NotLicensed");
    }

    [Fact]
    public void CheckCapability_Should_Succeed_When_CapabilityExists()
    {
        var license = CreateValidLicense();

        var result = license.CheckCapability("catalog:read", MidDate);

        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("catalog:read");
        result.Value.IsEnabled.Should().BeTrue();
    }

    // ─── TrackUsage ──────────────────────────────────────────────────

    [Fact]
    public void TrackUsage_Should_ReturnFailure_When_QuotaIsExceeded()
    {
        var license = CreateValidLicense();

        var result = license.TrackUsage("api.calls", 101, MidDate);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Licensing.Quota.Exceeded");
    }

    [Fact]
    public void TrackUsage_Should_Succeed_When_WithinQuota()
    {
        var license = CreateValidLicense();

        var result = license.TrackUsage("api.calls", 50, MidDate);

        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentUsage.Should().Be(50);
        result.Value.MetricCode.Should().Be("api.calls");
    }

    // ─── Deactivate ──────────────────────────────────────────────────

    [Fact]
    public void Deactivate_Should_SetIsActiveToFalse()
    {
        var license = CreateValidLicense();
        license.IsActive.Should().BeTrue();

        license.Deactivate();

        license.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_Should_PreventFurtherCapabilityChecks()
    {
        var license = CreateValidLicense();
        license.Deactivate();

        var result = license.CheckCapability("catalog:read", MidDate);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Licensing.License.Inactive");
    }

    // ─── IsExpired ───────────────────────────────────────────────────

    [Fact]
    public void IsExpired_Should_ReturnTrue_When_PastExpiration()
    {
        var license = LicenseAggregate.Create(
            "LIC-EXP", "Corp", BaseDate, BaseDate.AddDays(30), 1);
        var futureDate = BaseDate.AddDays(31);

        license.IsExpired(futureDate).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_Should_ReturnFalse_When_StillValid()
    {
        var license = LicenseAggregate.Create(
            "LIC-VAL", "Corp", BaseDate, BaseDate.AddDays(30), 1);
        var checkDate = BaseDate.AddDays(15);

        license.IsExpired(checkDate).Should().BeFalse();
    }

    // ─── IsInGracePeriod ─────────────────────────────────────────────

    [Fact]
    public void IsInGracePeriod_Should_ReturnTrue_DuringGracePeriod()
    {
        var expiresAt = BaseDate.AddDays(30);
        var license = LicenseAggregate.Create(
            "LIC-GP", "Corp", BaseDate, expiresAt, 1, gracePeriodDays: 7);

        // 3 dias após expiração — dentro do grace period de 7 dias
        var checkDate = expiresAt.AddDays(3);

        license.IsInGracePeriod(checkDate).Should().BeTrue();
    }

    [Fact]
    public void IsInGracePeriod_Should_ReturnFalse_AfterGracePeriod()
    {
        var expiresAt = BaseDate.AddDays(30);
        var license = LicenseAggregate.Create(
            "LIC-GP", "Corp", BaseDate, expiresAt, 1, gracePeriodDays: 7);

        // 10 dias após expiração — fora do grace period de 7 dias
        var checkDate = expiresAt.AddDays(10);

        license.IsInGracePeriod(checkDate).Should().BeFalse();
    }

    // ─── CalculateHealthScore ────────────────────────────────────────

    [Fact]
    public void CalculateHealthScore_Should_ReturnBetweenZeroAndOne()
    {
        var license = CreateValidLicense();

        var score = license.CalculateHealthScore(MidDate);

        score.Should().BeGreaterThanOrEqualTo(0.0m);
        score.Should().BeLessThanOrEqualTo(1.0m);
    }

    [Fact]
    public void CalculateHealthScore_Should_ReturnZero_When_Inactive()
    {
        var license = CreateValidLicense();
        license.Deactivate();

        var score = license.CalculateHealthScore(MidDate);

        score.Should().Be(0.0m);
    }

    // ─── CreateTrial ─────────────────────────────────────────────────

    [Fact]
    public void CreateTrial_Should_CreateWithCorrectDefaults()
    {
        var trial = LicenseAggregate.CreateTrial("TRIAL-001", "Startup Inc", BaseDate);

        trial.Type.Should().Be(LicenseType.Trial);
        trial.IsTrial.Should().BeTrue();
        trial.MaxActivations.Should().Be(1);
        trial.ExpiresAt.Should().Be(BaseDate.AddDays(30));
        trial.GracePeriodDays.Should().Be(7);
        trial.Edition.Should().Be(LicenseEdition.Professional);
        trial.IsActive.Should().BeTrue();
        trial.TrialConverted.Should().BeFalse();
        trial.TrialExtensionCount.Should().Be(0);
        // Quotas padrão: api.count(25), environment.count(2), user.count(5)
        trial.UsageQuotas.Should().HaveCount(3);
    }

    [Fact]
    public void CreateTrial_Should_AcceptCustomQuotas()
    {
        var customQuotas = new[]
        {
            UsageQuota.Create("api.count", 50, enforcementLevel: EnforcementLevel.Soft),
            UsageQuota.Create("user.count", 10, enforcementLevel: EnforcementLevel.Hard)
        };

        var trial = LicenseAggregate.CreateTrial("TRIAL-CQ", "Custom Corp", BaseDate, usageQuotas: customQuotas);

        trial.UsageQuotas.Should().HaveCount(2);
        trial.UsageQuotas.Should().Contain(q => q.MetricCode == "api.count" && q.Limit == 50);
        trial.UsageQuotas.Should().Contain(q => q.MetricCode == "user.count" && q.Limit == 10);
    }

    // ─── ExtendTrial ─────────────────────────────────────────────────

    [Fact]
    public void ExtendTrial_Should_Succeed_ForActiveTrialWithNoExtensions()
    {
        var trial = LicenseAggregate.CreateTrial("TRIAL-EXT", "TestCorp", BaseDate);
        var originalExpiry = trial.ExpiresAt;

        var result = trial.ExtendTrial(15, MidDate);

        result.IsSuccess.Should().BeTrue();
        trial.ExpiresAt.Should().Be(originalExpiry.AddDays(15));
        trial.TrialExtensionCount.Should().Be(1);
    }

    [Fact]
    public void ExtendTrial_Should_Fail_ForNonTrialLicense()
    {
        var license = CreateValidLicense(); // tipo Standard

        var result = license.ExtendTrial(15, MidDate);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Licensing.Trial.NotTrial");
    }

    [Fact]
    public void ExtendTrial_Should_Fail_ForInactiveLicense()
    {
        var trial = LicenseAggregate.CreateTrial("TRIAL-INACT", "TestCorp", BaseDate);
        trial.Deactivate();

        var result = trial.ExtendTrial(15, MidDate);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Licensing.License.Inactive");
    }

    [Fact]
    public void ExtendTrial_Should_Fail_WhenAlreadyConverted()
    {
        var trial = LicenseAggregate.CreateTrial("TRIAL-CONV", "TestCorp", BaseDate);
        _ = trial.ConvertTrial(LicenseEdition.Professional, BaseDate.AddYears(1), 5, 15, MidDate);

        var result = trial.ExtendTrial(15, MidDate);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Licensing.Trial.NotTrial");
    }

    [Fact]
    public void ExtendTrial_Should_Fail_WhenExtensionLimitReached()
    {
        var trial = LicenseAggregate.CreateTrial("TRIAL-LIM", "TestCorp", BaseDate);
        _ = trial.ExtendTrial(15, MidDate); // primeira extensão

        var result = trial.ExtendTrial(15, MidDate);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Licensing.Trial.ExtensionLimitReached");
    }

    // ─── ConvertTrial ────────────────────────────────────────────────

    [Fact]
    public void ConvertTrial_Should_Succeed_ForActiveTrial()
    {
        var trial = LicenseAggregate.CreateTrial("TRIAL-CNV", "TestCorp", BaseDate);
        var newExpiry = BaseDate.AddYears(1);

        var result = trial.ConvertTrial(LicenseEdition.Enterprise, newExpiry, 10, 30, MidDate);

        result.IsSuccess.Should().BeTrue();
        trial.Type.Should().Be(LicenseType.Standard);
        trial.Edition.Should().Be(LicenseEdition.Enterprise);
        trial.ExpiresAt.Should().Be(newExpiry);
        trial.MaxActivations.Should().Be(10);
        trial.GracePeriodDays.Should().Be(30);
        trial.TrialConverted.Should().BeTrue();
        trial.TrialConvertedAt.Should().Be(MidDate);
    }

    [Fact]
    public void ConvertTrial_Should_Fail_ForNonTrialLicense()
    {
        var license = CreateValidLicense(); // tipo Standard

        var result = license.ConvertTrial(LicenseEdition.Enterprise, BaseDate.AddYears(1), 10, 30, MidDate);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Licensing.Trial.NotTrial");
    }

    [Fact]
    public void ConvertTrial_Should_Fail_WhenAlreadyConverted()
    {
        var trial = LicenseAggregate.CreateTrial("TRIAL-DUP", "TestCorp", BaseDate);
        _ = trial.ConvertTrial(LicenseEdition.Professional, BaseDate.AddYears(1), 5, 15, MidDate);

        var result = trial.ConvertTrial(LicenseEdition.Enterprise, BaseDate.AddYears(2), 10, 30, MidDate);

        // Após conversão, Type muda para Standard; segunda tentativa falha com NotTrial
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Licensing.Trial.NotTrial");
    }

    // ─── Helper ──────────────────────────────────────────────────────

    private static LicenseAggregate CreateValidLicense()
        => LicenseAggregate.Create(
            "LIC-001",
            "Acme Corp",
            BaseDate,
            BaseDate.AddYears(1),
            2,
            [LicenseCapability.Create("catalog:read", "Catalog Read")],
            [UsageQuota.Create("api.calls", 100)]);
}
