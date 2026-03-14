using FluentAssertions;
using NexTraceOne.CommercialCatalog.Application.Abstractions;
using NexTraceOne.CommercialCatalog.Application.Features.CreateFeaturePack;
using NexTraceOne.CommercialCatalog.Application.Features.CreatePlan;
using NexTraceOne.CommercialCatalog.Application.Features.GenerateLicenseKey;
using NexTraceOne.CommercialCatalog.Application.Features.ListFeaturePacks;
using NexTraceOne.CommercialCatalog.Application.Features.ListPlans;
using NexTraceOne.CommercialCatalog.Domain.Entities;
using NexTraceOne.Licensing.Application.Abstractions;
using NexTraceOne.Licensing.Domain.Enums;
using NSubstitute;
using Xunit;
using License = NexTraceOne.Licensing.Domain.Entities.License;
using LicenseId = NexTraceOne.Licensing.Domain.Entities.LicenseId;

namespace NexTraceOne.CommercialGovernance.Tests.Application.Features;

/// <summary>
/// Testes de aplicação para as features do subdomínio CommercialCatalog.
/// Cobre handlers de CreatePlan, ListPlans, CreateFeaturePack, ListFeaturePacks
/// e GenerateLicenseKey usando NSubstitute para mocking de repositórios.
/// </summary>
public sealed class CommercialCatalogApplicationTests
{
    private readonly IPlanRepository _planRepository = Substitute.For<IPlanRepository>();
    private readonly IFeaturePackRepository _featurePackRepository = Substitute.For<IFeaturePackRepository>();
    private readonly ILicenseRepository _licenseRepository = Substitute.For<ILicenseRepository>();

    // ─── CreatePlan ──────────────────────────────────────────────────

    [Fact]
    public async Task CreatePlan_ValidCommand_ShouldSucceed()
    {
        _planRepository.GetByCodeAsync("enterprise", Arg.Any<CancellationToken>())
            .Returns((Plan?)null);

        var handler = new CreatePlan.Handler(_planRepository);
        var command = new CreatePlan.Command(
            "enterprise", "Enterprise", CommercialModel.Subscription,
            DeploymentModel.SelfHosted, 10, 30);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("enterprise");
        result.Value.Name.Should().Be("Enterprise");
        await _planRepository.Received(1).AddAsync(Arg.Any<Plan>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreatePlan_DuplicateCode_ShouldFail()
    {
        var existingPlan = Plan.Create(
            "enterprise", "Enterprise", CommercialModel.Subscription,
            DeploymentModel.SelfHosted, 10, 30);
        _planRepository.GetByCodeAsync("enterprise", Arg.Any<CancellationToken>())
            .Returns(existingPlan);

        var handler = new CreatePlan.Handler(_planRepository);
        var command = new CreatePlan.Command(
            "enterprise", "Enterprise v2", CommercialModel.Subscription,
            DeploymentModel.SelfHosted, 5, 15);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CodeAlreadyExists");
    }

    [Fact]
    public void CreatePlan_InvalidCommand_ShouldFailValidation()
    {
        var validator = new CreatePlan.Validator();
        var command = new CreatePlan.Command(
            "", "", CommercialModel.Subscription,
            DeploymentModel.SaaS, 0, -1);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    // ─── ListPlans ───────────────────────────────────────────────────

    [Fact]
    public async Task ListPlans_ShouldReturnPlans()
    {
        var plans = new List<Plan>
        {
            Plan.Create("starter", "Starter", CommercialModel.Subscription, DeploymentModel.SaaS, 1, 0),
            Plan.Create("enterprise", "Enterprise", CommercialModel.Perpetual, DeploymentModel.OnPremise, 10, 30)
        };
        _planRepository.ListAsync(null, Arg.Any<CancellationToken>())
            .Returns(plans.AsReadOnly());

        var handler = new ListPlans.Handler(_planRepository);
        var query = new ListPlans.Query();

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }

    // ─── CreateFeaturePack ───────────────────────────────────────────

    [Fact]
    public async Task CreateFeaturePack_ValidCommand_ShouldSucceed()
    {
        _featurePackRepository.GetByCodeAsync("governance-pack", Arg.Any<CancellationToken>())
            .Returns((FeaturePack?)null);

        var handler = new CreateFeaturePack.Handler(_featurePackRepository);
        var command = new CreateFeaturePack.Command(
            "governance-pack", "Governance Pack", "Pack de governança",
            [new CreateFeaturePack.ItemInput("catalog:read", "Catalog Read", 100)]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("governance-pack");
        await _featurePackRepository.Received(1).AddAsync(Arg.Any<FeaturePack>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateFeaturePack_DuplicateCode_ShouldFail()
    {
        var existing = FeaturePack.Create("governance-pack", "Existing Pack");
        _featurePackRepository.GetByCodeAsync("governance-pack", Arg.Any<CancellationToken>())
            .Returns(existing);

        var handler = new CreateFeaturePack.Handler(_featurePackRepository);
        var command = new CreateFeaturePack.Command(
            "governance-pack", "New Pack", null,
            [new CreateFeaturePack.ItemInput("catalog:write", "Catalog Write", null)]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CodeAlreadyExists");
    }

    // ─── ListFeaturePacks ────────────────────────────────────────────

    [Fact]
    public async Task ListFeaturePacks_ShouldReturnPacks()
    {
        var pack = FeaturePack.Create("pack-001", "Pack 001", "Description");
        pack.AddItem("catalog:read", "Catalog Read", 100);

        _featurePackRepository.ListAsync(null, Arg.Any<CancellationToken>())
            .Returns(new List<FeaturePack> { pack }.AsReadOnly());

        var handler = new ListFeaturePacks.Handler(_featurePackRepository);
        var query = new ListFeaturePacks.Query();

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
        result.Value.Items[0].Capabilities.Should().ContainSingle();
    }

    // ─── GenerateLicenseKey ──────────────────────────────────────────

    [Fact]
    public async Task GenerateLicenseKey_ValidLicense_ShouldGenerateKey()
    {
        var license = License.Create(
            "LIC-001", "Acme Corp",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1), 1);
        _licenseRepository.GetByIdAsync(Arg.Any<LicenseId>(), Arg.Any<CancellationToken>())
            .Returns(license);

        var handler = new GenerateLicenseKey.Handler(_licenseRepository);
        var command = new GenerateLicenseKey.Command(license.Id.Value);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.NewLicenseKey.Should().StartWith("NXKEY-");
        result.Value.LicenseId.Should().Be(license.Id.Value);
    }

    [Fact]
    public async Task GenerateLicenseKey_NonExistentLicense_ShouldFail()
    {
        _licenseRepository.GetByIdAsync(Arg.Any<LicenseId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<License?>(null));

        var handler = new GenerateLicenseKey.Handler(_licenseRepository);
        var command = new GenerateLicenseKey.Command(Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("LicenseNotFound");
    }
}
