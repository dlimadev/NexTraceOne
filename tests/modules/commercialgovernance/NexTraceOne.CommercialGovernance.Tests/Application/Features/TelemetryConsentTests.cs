using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Licensing.Application.Abstractions;
using NexTraceOne.Licensing.Domain.Entities;
using NexTraceOne.Licensing.Domain.Enums;
using LicenseAggregate = NexTraceOne.Licensing.Domain.Entities.License;
using GetTelemetryConsentFeature = NexTraceOne.Licensing.Application.Features.GetTelemetryConsent.GetTelemetryConsent;
using UpdateTelemetryConsentFeature = NexTraceOne.Licensing.Application.Features.UpdateTelemetryConsent.UpdateTelemetryConsent;

namespace NexTraceOne.Licensing.Tests.Application.Features;

/// <summary>
/// Testes dos handlers de TelemetryConsent.
/// Cobre: get (com e sem registro existente), update (grant, deny, partial),
/// criação automática quando não existe, e cenários de licença não encontrada.
/// </summary>
public sealed class TelemetryConsentTests
{
    private static readonly DateTimeOffset Now = new(2025, 01, 10, 10, 0, 0, TimeSpan.Zero);
    private const string LicenseKey = "LIC-TELEMETRY-001";

    // ─── GetTelemetryConsent ─────────────────────────────────────────

    [Fact]
    public async Task GetTelemetryConsent_Should_ReturnNotRequested_When_NoConsentExists()
    {
        var (repo, dt) = CreateMocks();
        var license = CreateActiveLicense();
        repo.GetByLicenseKeyAsync(LicenseKey, Arg.Any<CancellationToken>()).Returns(license);
        repo.GetTelemetryConsentByLicenseIdAsync(license.Id, Arg.Any<CancellationToken>())
            .Returns((TelemetryConsent?)null);

        var sut = new GetTelemetryConsentFeature.Handler(repo);

        var result = await sut.Handle(
            new GetTelemetryConsentFeature.Query(LicenseKey), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("NotRequested");
        result.Value.AllowUsageMetrics.Should().BeFalse();
        result.Value.AllowPerformanceData.Should().BeFalse();
        result.Value.AllowErrorDiagnostics.Should().BeFalse();
    }

    [Fact]
    public async Task GetTelemetryConsent_Should_ReturnExistingConsent_When_ConsentExists()
    {
        var (repo, dt) = CreateMocks();
        var license = CreateActiveLicense();
        var consent = TelemetryConsent.Create(
            license.Id, TelemetryConsentStatus.Granted, "admin", Now,
            "Full consent", true, true, true);

        repo.GetByLicenseKeyAsync(LicenseKey, Arg.Any<CancellationToken>()).Returns(license);
        repo.GetTelemetryConsentByLicenseIdAsync(license.Id, Arg.Any<CancellationToken>())
            .Returns(consent);

        var sut = new GetTelemetryConsentFeature.Handler(repo);

        var result = await sut.Handle(
            new GetTelemetryConsentFeature.Query(LicenseKey), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Granted");
        result.Value.AllowUsageMetrics.Should().BeTrue();
        result.Value.AllowPerformanceData.Should().BeTrue();
        result.Value.AllowErrorDiagnostics.Should().BeTrue();
        result.Value.UpdatedBy.Should().Be("admin");
        result.Value.Reason.Should().Be("Full consent");
    }

    [Fact]
    public async Task GetTelemetryConsent_Should_Fail_When_LicenseNotFound()
    {
        var (repo, dt) = CreateMocks();
        repo.GetByLicenseKeyAsync("MISSING", Arg.Any<CancellationToken>())
            .Returns((LicenseAggregate?)null);

        var sut = new GetTelemetryConsentFeature.Handler(repo);

        var result = await sut.Handle(
            new GetTelemetryConsentFeature.Query("MISSING"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Licensing.License.KeyNotFound");
    }

    // ─── UpdateTelemetryConsent — Grant ───────────────────────────────

    [Fact]
    public async Task UpdateTelemetryConsent_Grant_Should_CreateAndGrant_When_NoConsentExists()
    {
        var (repo, dt) = CreateMocks();
        var license = CreateActiveLicense();
        repo.GetByLicenseKeyAsync(LicenseKey, Arg.Any<CancellationToken>()).Returns(license);
        repo.GetTelemetryConsentByLicenseIdAsync(license.Id, Arg.Any<CancellationToken>())
            .Returns((TelemetryConsent?)null);

        var sut = new UpdateTelemetryConsentFeature.Handler(repo, dt);

        var result = await sut.Handle(
            new UpdateTelemetryConsentFeature.Command(
                LicenseKey, "grant", "tenant-admin", "Full consent"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Granted");
        result.Value.AllowUsageMetrics.Should().BeTrue();
        result.Value.AllowPerformanceData.Should().BeTrue();
        result.Value.AllowErrorDiagnostics.Should().BeTrue();
        result.Value.UpdatedBy.Should().Be("tenant-admin");

        repo.Received(1).AddTelemetryConsent(Arg.Any<TelemetryConsent>());
    }

    [Fact]
    public async Task UpdateTelemetryConsent_Grant_Should_UpdateExisting_When_ConsentExists()
    {
        var (repo, dt) = CreateMocks();
        var license = CreateActiveLicense();
        var consent = TelemetryConsent.Create(
            license.Id, TelemetryConsentStatus.Denied, "old-admin", Now.AddDays(-30));

        repo.GetByLicenseKeyAsync(LicenseKey, Arg.Any<CancellationToken>()).Returns(license);
        repo.GetTelemetryConsentByLicenseIdAsync(license.Id, Arg.Any<CancellationToken>())
            .Returns(consent);

        var sut = new UpdateTelemetryConsentFeature.Handler(repo, dt);

        var result = await sut.Handle(
            new UpdateTelemetryConsentFeature.Command(
                LicenseKey, "grant", "new-admin", "Re-granted after review"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Granted");
        result.Value.AllowUsageMetrics.Should().BeTrue();
        result.Value.AllowPerformanceData.Should().BeTrue();
        result.Value.AllowErrorDiagnostics.Should().BeTrue();

        repo.DidNotReceive().AddTelemetryConsent(Arg.Any<TelemetryConsent>());
    }

    // ─── UpdateTelemetryConsent — Deny ───────────────────────────────

    [Fact]
    public async Task UpdateTelemetryConsent_Deny_Should_RevokeAll()
    {
        var (repo, dt) = CreateMocks();
        var license = CreateActiveLicense();
        var consent = TelemetryConsent.Create(
            license.Id, TelemetryConsentStatus.Granted, "admin", Now,
            allowUsageMetrics: true, allowPerformanceData: true, allowErrorDiagnostics: true);

        repo.GetByLicenseKeyAsync(LicenseKey, Arg.Any<CancellationToken>()).Returns(license);
        repo.GetTelemetryConsentByLicenseIdAsync(license.Id, Arg.Any<CancellationToken>())
            .Returns(consent);

        var sut = new UpdateTelemetryConsentFeature.Handler(repo, dt);

        var result = await sut.Handle(
            new UpdateTelemetryConsentFeature.Command(
                LicenseKey, "deny", "dpo", "Privacy policy change"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Denied");
        result.Value.AllowUsageMetrics.Should().BeFalse();
        result.Value.AllowPerformanceData.Should().BeFalse();
        result.Value.AllowErrorDiagnostics.Should().BeFalse();
    }

    // ─── UpdateTelemetryConsent — Partial ─────────────────────────────

    [Fact]
    public async Task UpdateTelemetryConsent_Partial_Should_SetSelectiveFlags()
    {
        var (repo, dt) = CreateMocks();
        var license = CreateActiveLicense();
        repo.GetByLicenseKeyAsync(LicenseKey, Arg.Any<CancellationToken>()).Returns(license);
        repo.GetTelemetryConsentByLicenseIdAsync(license.Id, Arg.Any<CancellationToken>())
            .Returns((TelemetryConsent?)null);

        var sut = new UpdateTelemetryConsentFeature.Handler(repo, dt);

        var result = await sut.Handle(
            new UpdateTelemetryConsentFeature.Command(
                LicenseKey, "partial", "admin", "Only metrics",
                AllowUsageMetrics: true,
                AllowPerformanceData: false,
                AllowErrorDiagnostics: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Partial");
        result.Value.AllowUsageMetrics.Should().BeTrue();
        result.Value.AllowPerformanceData.Should().BeFalse();
        result.Value.AllowErrorDiagnostics.Should().BeTrue();
    }

    // ─── UpdateTelemetryConsent — License not found ───────────────────

    [Fact]
    public async Task UpdateTelemetryConsent_Should_Fail_When_LicenseNotFound()
    {
        var (repo, dt) = CreateMocks();
        repo.GetByLicenseKeyAsync("MISSING", Arg.Any<CancellationToken>())
            .Returns((LicenseAggregate?)null);

        var sut = new UpdateTelemetryConsentFeature.Handler(repo, dt);

        var result = await sut.Handle(
            new UpdateTelemetryConsentFeature.Command("MISSING", "grant", "admin"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Licensing.License.KeyNotFound");
    }

    // ─── Validator ────────────────────────────────────────────────────

    [Theory]
    [InlineData("grant")]
    [InlineData("deny")]
    [InlineData("partial")]
    public void UpdateTelemetryConsent_Validator_Should_AcceptValidActions(string action)
    {
        var validator = new UpdateTelemetryConsentFeature.Validator();
        var command = new UpdateTelemetryConsentFeature.Command(
            "LIC-001", action, "admin");

        var result = validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateTelemetryConsent_Validator_Should_RejectInvalidAction()
    {
        var validator = new UpdateTelemetryConsentFeature.Validator();
        var command = new UpdateTelemetryConsentFeature.Command(
            "LIC-001", "invalid", "admin");

        var result = validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetTelemetryConsent_Validator_Should_RejectEmptyLicenseKey()
    {
        var validator = new GetTelemetryConsentFeature.Validator();
        var query = new GetTelemetryConsentFeature.Query("");

        var result = validator.Validate(query);
        result.IsValid.Should().BeFalse();
    }

    // ─── Helpers ──────────────────────────────────────────────────────

    private static LicenseAggregate CreateActiveLicense()
    {
        return LicenseAggregate.Create(
            LicenseKey,
            "Test Corp",
            Now.AddMonths(-1),
            Now.AddYears(1),
            5,
            [LicenseCapability.Create("catalog:read", "Catalog Read")],
            [UsageQuota.Create("api.count", 100)],
            deploymentModel: DeploymentModel.SaaS);
    }

    private static (ILicenseRepository repo, IDateTimeProvider dt) CreateMocks()
    {
        var repo = Substitute.For<ILicenseRepository>();
        var dt = Substitute.For<IDateTimeProvider>();
        dt.UtcNow.Returns(Now);
        return (repo, dt);
    }
}
