using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;

using CheckDeployReadinessFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.CheckDeployReadiness.CheckDeployReadiness;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>Testes unitários para a feature CheckDeployReadiness.</summary>
public sealed class CheckDeployReadinessTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static Release CreateRelease() =>
        Release.Create(Guid.NewGuid(), Guid.Empty, "TestService", "1.0.0", "staging", "https://ci/pipeline/1", "abc123def456", FixedNow);

    private static EffectiveConfigurationDto CreateConfig(string key, string? value) =>
        new(key, value, "Tenant", null, false, false, key, "string", false, 1);

    private static (IReleaseRepository repo, IConfigurationResolutionService config, IDateTimeProvider dt) CreateMocks()
    {
        var repo = Substitute.For<IReleaseRepository>();
        var config = Substitute.For<IConfigurationResolutionService>();
        var dt = Substitute.For<IDateTimeProvider>();
        dt.UtcNow.Returns(FixedNow);
        return (repo, config, dt);
    }

    [Fact]
    public async Task Should_Return_IsReady_True_When_No_Checks_Configured()
    {
        var (repo, config, dt) = CreateMocks();
        var release = CreateRelease();
        repo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(release);

        config.ResolveEffectiveValueAsync(Arg.Any<string>(), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);

        var sut = new CheckDeployReadinessFeature.Handler(repo, config, dt);
        var result = await sut.Handle(new CheckDeployReadinessFeature.Query(release.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsReady.Should().BeTrue();
        result.Value.TotalChecks.Should().Be(0);
        result.Value.PassedChecks.Should().Be(0);
        result.Value.FailedChecks.Should().Be(0);
    }

    [Fact]
    public async Task Should_Pass_Approval_Check_When_Release_Is_Approved()
    {
        var (repo, config, dt) = CreateMocks();
        var release = CreateRelease();
        release.SetApprovalStatus("Approved");
        repo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(release);

        config.ResolveEffectiveValueAsync(
            "change.deploy.require_release_approval", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("change.deploy.require_release_approval", "true"));
        config.ResolveEffectiveValueAsync(
            Arg.Is<string>(k => k != "change.deploy.require_release_approval"), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);

        var sut = new CheckDeployReadinessFeature.Handler(repo, config, dt);
        var result = await sut.Handle(new CheckDeployReadinessFeature.Query(release.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsReady.Should().BeTrue();
        result.Value.Checks.Should().ContainSingle(c => c.CheckId == "release_approval" && c.Passed);
    }

    [Fact]
    public async Task Should_Fail_Approval_Check_When_Release_Is_Not_Approved()
    {
        var (repo, config, dt) = CreateMocks();
        var release = CreateRelease();
        repo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(release);

        config.ResolveEffectiveValueAsync(
            "change.deploy.require_release_approval", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("change.deploy.require_release_approval", "true"));
        config.ResolveEffectiveValueAsync(
            Arg.Is<string>(k => k != "change.deploy.require_release_approval"), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);

        var sut = new CheckDeployReadinessFeature.Handler(repo, config, dt);
        var result = await sut.Handle(new CheckDeployReadinessFeature.Query(release.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsReady.Should().BeFalse();
        result.Value.FailedChecks.Should().Be(1);
        result.Value.Checks.Should().ContainSingle(c => c.CheckId == "release_approval" && !c.Passed);
    }

    [Fact]
    public async Task Should_Fail_When_Breaking_Changes_Detected_And_Block_Enabled()
    {
        var (repo, config, dt) = CreateMocks();
        var release = CreateRelease();
        release.SetHasBreakingChanges(true);
        repo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(release);

        config.ResolveEffectiveValueAsync(
            "catalog.contract.breaking_change.block_deploy", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("catalog.contract.breaking_change.block_deploy", "true"));
        config.ResolveEffectiveValueAsync(
            Arg.Is<string>(k => k != "catalog.contract.breaking_change.block_deploy"), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);

        var sut = new CheckDeployReadinessFeature.Handler(repo, config, dt);
        var result = await sut.Handle(new CheckDeployReadinessFeature.Query(release.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsReady.Should().BeFalse();
        result.Value.Checks.Should().ContainSingle(c => c.CheckId == "no_breaking_changes" && !c.Passed);
    }

    [Fact]
    public async Task Should_Be_Ready_When_All_Configured_Checks_Pass()
    {
        var (repo, config, dt) = CreateMocks();
        var release = CreateRelease();
        release.SetApprovalStatus("Approved");
        release.SetExternalValidationPassed(true);
        repo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(release);

        config.ResolveEffectiveValueAsync(
            "change.deploy.require_release_approval", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("change.deploy.require_release_approval", "true"));
        config.ResolveEffectiveValueAsync(
            "catalog.contract.breaking_change.block_deploy", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("catalog.contract.breaking_change.block_deploy", "true"));
        config.ResolveEffectiveValueAsync(
            "change.release.external_validation.enabled", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("change.release.external_validation.enabled", "true"));
        config.ResolveEffectiveValueAsync(
            "change.deploy.pre_deploy_checks", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);

        var sut = new CheckDeployReadinessFeature.Handler(repo, config, dt);
        var result = await sut.Handle(new CheckDeployReadinessFeature.Query(release.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsReady.Should().BeTrue();
        result.Value.TotalChecks.Should().Be(3);
        result.Value.PassedChecks.Should().Be(3);
        result.Value.FailedChecks.Should().Be(0);
    }

    [Fact]
    public async Task Should_Return_Error_When_Release_Not_Found()
    {
        var (repo, config, dt) = CreateMocks();
        repo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns((Release?)null);

        var sut = new CheckDeployReadinessFeature.Handler(repo, config, dt);
        var result = await sut.Handle(new CheckDeployReadinessFeature.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Release.NotFound");
    }

    [Fact]
    public async Task Should_Include_PreDeploy_Checks_When_Configured()
    {
        var (repo, config, dt) = CreateMocks();
        var release = CreateRelease();
        repo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>()).Returns(release);

        config.ResolveEffectiveValueAsync(
            "change.deploy.pre_deploy_checks", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("change.deploy.pre_deploy_checks", """{"contract_compliance":true,"security_scan":true,"evidence_pack":false}"""));
        config.ResolveEffectiveValueAsync(
            Arg.Is<string>(k => k != "change.deploy.pre_deploy_checks"), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);

        var sut = new CheckDeployReadinessFeature.Handler(repo, config, dt);
        var result = await sut.Handle(new CheckDeployReadinessFeature.Query(release.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Checks.Should().HaveCount(2);
        result.Value.Checks.Should().Contain(c => c.CheckId == "contract_compliance");
        result.Value.Checks.Should().Contain(c => c.CheckId == "security_scan");
        result.Value.Checks.Should().NotContain(c => c.CheckId == "evidence_pack");
    }
}
