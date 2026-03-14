using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Licensing.Application.Abstractions;
using NexTraceOne.Licensing.Domain.Entities;
using NexTraceOne.Licensing.Domain.Enums;
using LicenseAggregate = NexTraceOne.Licensing.Domain.Entities.License;
using IssueLicenseFeature = NexTraceOne.Licensing.Application.Features.IssueLicense.IssueLicense;
using RevokeLicenseFeature = NexTraceOne.Licensing.Application.Features.RevokeLicense.RevokeLicense;
using RehostLicenseFeature = NexTraceOne.Licensing.Application.Features.RehostLicense.RehostLicense;
using ListLicensesFeature = NexTraceOne.Licensing.Application.Features.ListLicenses.ListLicenses;

namespace NexTraceOne.Licensing.Tests.Application.Features;

/// <summary>
/// Testes de handlers para operações de vendor ops do módulo Licensing.
/// Cobre: IssueLicense, RevokeLicense, RehostLicense, ListLicenses.
/// </summary>
public sealed class VendorOpsApplicationTests
{
    private static readonly DateTimeOffset Now = new(2025, 01, 10, 10, 0, 0, TimeSpan.Zero);
    private const string LicenseKey = "LIC-001";
    private const string Fingerprint = "fingerprint-001";

    // ─── IssueLicense ─────────────────────────────────────────────────

    [Fact]
    public async Task IssueLicense_Should_CreateLicenseWithCorrectProperties()
    {
        var (repo, dt) = CreateMocks();
        var sut = new IssueLicenseFeature.Handler(repo, dt);

        var result = await sut.Handle(
            new IssueLicenseFeature.Command(
                "Acme Corp", 365, 5,
                LicenseType.Standard, LicenseEdition.Professional,
                15, DeploymentModel.SaaS, ActivationMode.Online,
                CommercialModel.Subscription, MeteringMode.RealTime),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CustomerName.Should().Be("Acme Corp");
        result.Value.LicenseType.Should().Be("Standard");
        result.Value.Edition.Should().Be("Professional");
        result.Value.DeploymentModel.Should().Be("SaaS");
        result.Value.LicenseKey.Should().StartWith("LIC-");
        repo.Received(1).Add(Arg.Any<LicenseAggregate>());
    }

    [Fact]
    public async Task IssueLicense_Should_CreateOnPremiseLicense()
    {
        var (repo, dt) = CreateMocks();
        var sut = new IssueLicenseFeature.Handler(repo, dt);

        var result = await sut.Handle(
            new IssueLicenseFeature.Command(
                "BigBank", 730, 3,
                LicenseType.Enterprise, LicenseEdition.Enterprise,
                30, DeploymentModel.OnPremise, ActivationMode.Offline,
                CommercialModel.Perpetual, MeteringMode.Manual),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DeploymentModel.Should().Be("OnPremise");
        result.Value.LicenseType.Should().Be("Enterprise");
    }

    [Fact]
    public async Task IssueLicense_Should_CreateSelfHostedLicense()
    {
        var (repo, dt) = CreateMocks();
        var sut = new IssueLicenseFeature.Handler(repo, dt);

        var result = await sut.Handle(
            new IssueLicenseFeature.Command(
                "TelcoCorp", 365, 2,
                LicenseType.Standard, LicenseEdition.Professional,
                15, DeploymentModel.SelfHosted, ActivationMode.Hybrid,
                CommercialModel.Subscription, MeteringMode.Periodic),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DeploymentModel.Should().Be("SelfHosted");
    }

    // ─── RevokeLicense ────────────────────────────────────────────────

    [Fact]
    public async Task RevokeLicense_Should_RevokeLicense_WhenActive()
    {
        var (repo, _) = CreateMocks();
        var license = CreateActiveLicense();
        repo.GetByLicenseKeyAsync(LicenseKey, Arg.Any<CancellationToken>()).Returns(license);
        var sut = new RevokeLicenseFeature.Handler(repo);

        var result = await sut.Handle(
            new RevokeLicenseFeature.Command(LicenseKey),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        license.IsActive.Should().BeFalse();
        license.Status.Should().Be(LicenseStatus.Revoked);
    }

    [Fact]
    public async Task RevokeLicense_Should_Fail_WhenNotFound()
    {
        var (repo, _) = CreateMocks();
        repo.GetByLicenseKeyAsync(LicenseKey, Arg.Any<CancellationToken>()).Returns((LicenseAggregate?)null);
        var sut = new RevokeLicenseFeature.Handler(repo);

        var result = await sut.Handle(
            new RevokeLicenseFeature.Command(LicenseKey),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Licensing.License.KeyNotFound");
    }

    [Fact]
    public async Task RevokeLicense_Should_Fail_WhenAlreadyInactive()
    {
        var (repo, _) = CreateMocks();
        var license = CreateActiveLicense();
        license.Deactivate();
        repo.GetByLicenseKeyAsync(LicenseKey, Arg.Any<CancellationToken>()).Returns(license);
        var sut = new RevokeLicenseFeature.Handler(repo);

        var result = await sut.Handle(
            new RevokeLicenseFeature.Command(LicenseKey),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Licensing.License.AlreadyRevoked");
    }

    // ─── RehostLicense ────────────────────────────────────────────────

    [Fact]
    public async Task RehostLicense_Should_ClearHardwareBinding()
    {
        var (repo, _) = CreateMocks();
        var license = CreateActivatedLicense();
        repo.GetByLicenseKeyAsync(LicenseKey, Arg.Any<CancellationToken>()).Returns(license);
        var sut = new RehostLicenseFeature.Handler(repo);

        var result = await sut.Handle(
            new RehostLicenseFeature.Command(LicenseKey),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        license.HardwareBinding.Should().BeNull();
        license.Status.Should().Be(LicenseStatus.PendingActivation);
    }

    [Fact]
    public async Task RehostLicense_Should_Fail_WhenNotFound()
    {
        var (repo, _) = CreateMocks();
        repo.GetByLicenseKeyAsync(LicenseKey, Arg.Any<CancellationToken>()).Returns((LicenseAggregate?)null);
        var sut = new RehostLicenseFeature.Handler(repo);

        var result = await sut.Handle(
            new RehostLicenseFeature.Command(LicenseKey),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task RehostLicense_Should_Fail_WhenInactive()
    {
        var (repo, _) = CreateMocks();
        var license = CreateActiveLicense();
        license.Deactivate();
        repo.GetByLicenseKeyAsync(LicenseKey, Arg.Any<CancellationToken>()).Returns(license);
        var sut = new RehostLicenseFeature.Handler(repo);

        var result = await sut.Handle(
            new RehostLicenseFeature.Command(LicenseKey),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // ─── ListLicenses ─────────────────────────────────────────────────

    [Fact]
    public async Task ListLicenses_Should_ReturnPaginatedResults()
    {
        var (repo, _) = CreateMocks();
        var licenses = new List<LicenseAggregate>
        {
            CreateActiveLicense(),
            LicenseAggregate.CreateTrial("TRIAL-001", "Startup Inc", Now)
        };
        repo.ListAsync(1, 20, Arg.Any<CancellationToken>())
            .Returns((licenses.AsReadOnly() as IReadOnlyList<LicenseAggregate>, 2));
        var sut = new ListLicensesFeature.Handler(repo);

        var result = await sut.Handle(
            new ListLicensesFeature.Query(1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Page.Should().Be(1);
    }

    [Fact]
    public async Task ListLicenses_Should_ReturnEmpty_WhenNoLicenses()
    {
        var (repo, _) = CreateMocks();
        repo.ListAsync(1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<LicenseAggregate>().AsReadOnly() as IReadOnlyList<LicenseAggregate>, 0));
        var sut = new ListLicensesFeature.Handler(repo);

        var result = await sut.Handle(
            new ListLicensesFeature.Query(1, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    // ─── Helpers ─────────────────────────────────────────────────────

    private static (ILicenseRepository repo, IDateTimeProvider dt) CreateMocks()
    {
        var repo = Substitute.For<ILicenseRepository>();
        var dt = Substitute.For<IDateTimeProvider>();
        dt.UtcNow.Returns(Now);
        return (repo, dt);
    }

    private static LicenseAggregate CreateActiveLicense()
        => LicenseAggregate.Create(
            LicenseKey,
            "Acme Corp",
            new DateTimeOffset(2025, 01, 01, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 01, 01, 0, 0, 0, TimeSpan.Zero),
            2,
            [LicenseCapability.Create("catalog:read", "Catalog Read")],
            [UsageQuota.Create("api.calls", 100)]);

    private static LicenseAggregate CreateActivatedLicense()
    {
        var license = CreateActiveLicense();
        _ = license.Activate(Fingerprint, "system", Now);
        return license;
    }
}
