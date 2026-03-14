using FluentAssertions;
using NexTraceOne.Licensing.Domain.Entities;
using NexTraceOne.Licensing.Domain.Enums;
using LicenseAggregate = NexTraceOne.Licensing.Domain.Entities.License;

namespace NexTraceOne.Licensing.Tests.Domain.Entities;

/// <summary>
/// Testes de domínio para funcionalidades adicionadas ao módulo CommercialGovernance:
/// - Propriedades de deployment model, activation mode, commercial model
/// - Revoke e Rehost de licenças
/// - TelemetryConsent
/// - LicenseStatus
/// </summary>
public sealed class LicenseExtendedTests
{
    private static readonly DateTimeOffset BaseDate = new(2025, 01, 01, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset MidDate = new(2025, 01, 10, 10, 0, 0, TimeSpan.Zero);

    // ─── DeploymentModel / ActivationMode / CommercialModel ──────────

    [Fact]
    public void Create_Should_SetDefaultDeploymentModel_ToSaaS()
    {
        var license = LicenseAggregate.Create(
            "LIC-001", "Acme", BaseDate, BaseDate.AddYears(1), 1);

        license.DeploymentModel.Should().Be(DeploymentModel.SaaS);
        license.ActivationMode.Should().Be(ActivationMode.Online);
        license.CommercialModel.Should().Be(CommercialModel.Subscription);
        license.MeteringMode.Should().Be(MeteringMode.RealTime);
    }

    [Fact]
    public void Create_Should_SetCustomDeploymentModel()
    {
        var license = LicenseAggregate.Create(
            "LIC-ONPREM", "BigBank", BaseDate, BaseDate.AddYears(1), 3,
            deploymentModel: DeploymentModel.OnPremise,
            activationMode: ActivationMode.Offline,
            commercialModel: CommercialModel.Perpetual,
            meteringMode: MeteringMode.Manual);

        license.DeploymentModel.Should().Be(DeploymentModel.OnPremise);
        license.ActivationMode.Should().Be(ActivationMode.Offline);
        license.CommercialModel.Should().Be(CommercialModel.Perpetual);
        license.MeteringMode.Should().Be(MeteringMode.Manual);
    }

    [Fact]
    public void Create_Should_SetSelfHostedDeploymentModel()
    {
        var license = LicenseAggregate.Create(
            "LIC-SH", "TelcoCorp", BaseDate, BaseDate.AddYears(1), 2,
            deploymentModel: DeploymentModel.SelfHosted,
            activationMode: ActivationMode.Hybrid,
            commercialModel: CommercialModel.Subscription,
            meteringMode: MeteringMode.Periodic);

        license.DeploymentModel.Should().Be(DeploymentModel.SelfHosted);
        license.ActivationMode.Should().Be(ActivationMode.Hybrid);
        license.CommercialModel.Should().Be(CommercialModel.Subscription);
        license.MeteringMode.Should().Be(MeteringMode.Periodic);
    }

    // ─── LicenseStatus ──────────────────────────────────────────────

    [Fact]
    public void Create_Should_SetStatus_ToPendingActivation()
    {
        var license = LicenseAggregate.Create(
            "LIC-001", "Acme", BaseDate, BaseDate.AddYears(1), 1);

        license.Status.Should().Be(LicenseStatus.PendingActivation);
    }

    [Fact]
    public void Activate_Should_SetStatus_ToActive()
    {
        var license = LicenseAggregate.Create(
            "LIC-001", "Acme", BaseDate, BaseDate.AddYears(1), 1);

        var result = license.Activate("hw-fingerprint-123", "system", MidDate);

        result.IsSuccess.Should().BeTrue();
        license.Status.Should().Be(LicenseStatus.Active);
    }

    // ─── Revoke ─────────────────────────────────────────────────────

    [Fact]
    public void Revoke_Should_DeactivateAndSetStatusRevoked()
    {
        var license = LicenseAggregate.Create(
            "LIC-001", "Acme", BaseDate, BaseDate.AddYears(1), 1);

        license.Revoke();

        license.IsActive.Should().BeFalse();
        license.Status.Should().Be(LicenseStatus.Revoked);
    }

    [Fact]
    public void Revoke_Should_PreventFurtherCapabilityChecks()
    {
        var license = LicenseAggregate.Create(
            "LIC-001", "Acme", BaseDate, BaseDate.AddYears(1), 1,
            [LicenseCapability.Create("catalog:read", "Catalog Read")]);

        license.Revoke();

        var result = license.CheckCapability("catalog:read", MidDate);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Licensing.License.Inactive");
    }

    // ─── Rehost ─────────────────────────────────────────────────────

    [Fact]
    public void Rehost_Should_ClearHardwareBinding_WhenActive()
    {
        var license = LicenseAggregate.Create(
            "LIC-001", "Acme", BaseDate, BaseDate.AddYears(1), 2);

        license.Activate("hw-001", "system", MidDate);
        license.HardwareBinding.Should().NotBeNull();

        var result = license.Rehost();

        result.IsSuccess.Should().BeTrue();
        license.HardwareBinding.Should().BeNull();
        license.Status.Should().Be(LicenseStatus.PendingActivation);
    }

    [Fact]
    public void Rehost_Should_Fail_WhenLicenseIsInactive()
    {
        var license = LicenseAggregate.Create(
            "LIC-001", "Acme", BaseDate, BaseDate.AddYears(1), 1);

        license.Deactivate();

        var result = license.Rehost();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Licensing.License.Inactive");
    }

    [Fact]
    public void Rehost_Should_PreserveActivationHistory()
    {
        var license = LicenseAggregate.Create(
            "LIC-001", "Acme", BaseDate, BaseDate.AddYears(1), 5);

        license.Activate("hw-001", "user1", MidDate);
        license.Activations.Should().HaveCount(1);

        license.Rehost();

        // Histórico de ativações é preservado para auditoria
        license.Activations.Should().HaveCount(1);
        license.HardwareBinding.Should().BeNull();
    }

    [Fact]
    public void Rehost_Then_Activate_Should_BindToNewHardware()
    {
        var license = LicenseAggregate.Create(
            "LIC-001", "Acme", BaseDate, BaseDate.AddYears(1), 5);

        license.Activate("hw-old", "system", MidDate);
        license.Rehost();
        var result = license.Activate("hw-new", "system", MidDate.AddDays(1));

        result.IsSuccess.Should().BeTrue();
        license.HardwareBinding!.Fingerprint.Should().Be("hw-new");
    }

    // ─── Deactivate / Status ────────────────────────────────────────

    [Fact]
    public void Deactivate_Should_SetStatus_ToSuspended()
    {
        var license = LicenseAggregate.Create(
            "LIC-001", "Acme", BaseDate, BaseDate.AddYears(1), 1);

        license.Deactivate();

        license.IsActive.Should().BeFalse();
        license.Status.Should().Be(LicenseStatus.Suspended);
    }

    // ─── TelemetryConsent ────────────────────────────────────────────

    [Fact]
    public void TelemetryConsent_Create_Should_SetInitialValues()
    {
        var licenseId = LicenseId.New();
        var consent = TelemetryConsent.Create(
            licenseId,
            TelemetryConsentStatus.NotRequested,
            "admin",
            BaseDate);

        consent.LicenseId.Should().Be(licenseId);
        consent.Status.Should().Be(TelemetryConsentStatus.NotRequested);
        consent.AllowUsageMetrics.Should().BeFalse();
        consent.AllowPerformanceData.Should().BeFalse();
        consent.AllowErrorDiagnostics.Should().BeFalse();
        consent.UpdatedBy.Should().Be("admin");
    }

    [Fact]
    public void TelemetryConsent_Grant_Should_EnableAllData()
    {
        var consent = TelemetryConsent.Create(
            LicenseId.New(), TelemetryConsentStatus.NotRequested, "admin", BaseDate);

        consent.Grant("tenant-admin", MidDate, "Accepted terms of service");

        consent.Status.Should().Be(TelemetryConsentStatus.Granted);
        consent.AllowUsageMetrics.Should().BeTrue();
        consent.AllowPerformanceData.Should().BeTrue();
        consent.AllowErrorDiagnostics.Should().BeTrue();
        consent.Reason.Should().Be("Accepted terms of service");
        consent.UpdatedBy.Should().Be("tenant-admin");
    }

    [Fact]
    public void TelemetryConsent_Deny_Should_DisableAllData()
    {
        var consent = TelemetryConsent.Create(
            LicenseId.New(), TelemetryConsentStatus.Granted, "admin", BaseDate,
            allowUsageMetrics: true, allowPerformanceData: true, allowErrorDiagnostics: true);

        consent.Deny("tenant-admin", MidDate, "Privacy concerns");

        consent.Status.Should().Be(TelemetryConsentStatus.Denied);
        consent.AllowUsageMetrics.Should().BeFalse();
        consent.AllowPerformanceData.Should().BeFalse();
        consent.AllowErrorDiagnostics.Should().BeFalse();
    }

    [Fact]
    public void TelemetryConsent_GrantPartial_Should_EnableSelectedData()
    {
        var consent = TelemetryConsent.Create(
            LicenseId.New(), TelemetryConsentStatus.NotRequested, "admin", BaseDate);

        consent.GrantPartial("tenant-admin", MidDate,
            allowUsageMetrics: true,
            allowPerformanceData: false,
            allowErrorDiagnostics: true,
            reason: "Only aggregated metrics");

        consent.Status.Should().Be(TelemetryConsentStatus.Partial);
        consent.AllowUsageMetrics.Should().BeTrue();
        consent.AllowPerformanceData.Should().BeFalse();
        consent.AllowErrorDiagnostics.Should().BeTrue();
    }

    // ─── Enum Values ─────────────────────────────────────────────────

    [Theory]
    [InlineData(DeploymentModel.SaaS, 0)]
    [InlineData(DeploymentModel.SelfHosted, 1)]
    [InlineData(DeploymentModel.OnPremise, 2)]
    public void DeploymentModel_Should_HaveExpectedValues(DeploymentModel model, int expected)
    {
        ((int)model).Should().Be(expected);
    }

    [Theory]
    [InlineData(ActivationMode.Online, 0)]
    [InlineData(ActivationMode.Offline, 1)]
    [InlineData(ActivationMode.Hybrid, 2)]
    public void ActivationMode_Should_HaveExpectedValues(ActivationMode mode, int expected)
    {
        ((int)mode).Should().Be(expected);
    }

    [Theory]
    [InlineData(CommercialModel.Perpetual, 0)]
    [InlineData(CommercialModel.Subscription, 1)]
    [InlineData(CommercialModel.UsageBased, 2)]
    [InlineData(CommercialModel.Trial, 3)]
    [InlineData(CommercialModel.Internal, 4)]
    public void CommercialModel_Should_HaveExpectedValues(CommercialModel model, int expected)
    {
        ((int)model).Should().Be(expected);
    }

    [Theory]
    [InlineData(MeteringMode.RealTime, 0)]
    [InlineData(MeteringMode.Periodic, 1)]
    [InlineData(MeteringMode.Manual, 2)]
    [InlineData(MeteringMode.Disabled, 3)]
    public void MeteringMode_Should_HaveExpectedValues(MeteringMode mode, int expected)
    {
        ((int)mode).Should().Be(expected);
    }

    [Theory]
    [InlineData(LicenseStatus.Active, 0)]
    [InlineData(LicenseStatus.GracePeriod, 1)]
    [InlineData(LicenseStatus.Expired, 2)]
    [InlineData(LicenseStatus.Suspended, 3)]
    [InlineData(LicenseStatus.Revoked, 4)]
    [InlineData(LicenseStatus.PendingActivation, 5)]
    public void LicenseStatus_Should_HaveExpectedValues(LicenseStatus status, int expected)
    {
        ((int)status).Should().Be(expected);
    }

    [Theory]
    [InlineData(TelemetryConsentStatus.NotRequested, 0)]
    [InlineData(TelemetryConsentStatus.Granted, 1)]
    [InlineData(TelemetryConsentStatus.Denied, 2)]
    [InlineData(TelemetryConsentStatus.Partial, 3)]
    public void TelemetryConsentStatus_Should_HaveExpectedValues(TelemetryConsentStatus status, int expected)
    {
        ((int)status).Should().Be(expected);
    }
}
