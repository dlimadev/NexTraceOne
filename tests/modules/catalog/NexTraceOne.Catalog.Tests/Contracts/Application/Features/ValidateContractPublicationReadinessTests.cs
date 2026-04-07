using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;

using ValidateFeature = NexTraceOne.Catalog.Application.Contracts.Features.ValidateContractPublicationReadiness.ValidateContractPublicationReadiness;

namespace NexTraceOne.Catalog.Tests.Contracts.Application.Features;

/// <summary>
/// Testes unitários para ValidateContractPublicationReadiness — parametrização de
/// publicação de contratos com 3 checks configuráveis.
/// </summary>
public sealed class ValidateContractPublicationReadinessTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static EffectiveConfigurationDto CreateConfig(string key, string? value) =>
        new(key, value, "Tenant", null, false, false, key, "string", false, 1);

    private static (IContractVersionRepository repo, IConfigurationResolutionService config, IDateTimeProvider dt) CreateMocks()
    {
        var repo = Substitute.For<IContractVersionRepository>();
        var config = Substitute.For<IConfigurationResolutionService>();
        var dt = Substitute.For<IDateTimeProvider>();
        dt.UtcNow.Returns(FixedNow);
        return (repo, config, dt);
    }

    private static ContractVersion CreateVersion(
        string specContent = """{"openapi":"3.0.0","paths":{}}""",
        ContractLifecycleState state = ContractLifecycleState.Draft)
    {
        var result = ContractVersion.Import(
            Guid.NewGuid(),
            "1.0.0",
            specContent,
            "json",
            "test-import");

        var version = result.Value;

        // Follow valid transitions: Draft → InReview → Approved → Locked → ...
        if (state >= ContractLifecycleState.InReview)
            version.TransitionTo(ContractLifecycleState.InReview, FixedNow);
        if (state >= ContractLifecycleState.Approved)
            version.TransitionTo(ContractLifecycleState.Approved, FixedNow);
        if (state >= ContractLifecycleState.Locked)
            version.TransitionTo(ContractLifecycleState.Locked, FixedNow);
        if (state >= ContractLifecycleState.Deprecated)
            version.TransitionTo(ContractLifecycleState.Deprecated, FixedNow);
        if (state >= ContractLifecycleState.Sunset)
            version.TransitionTo(ContractLifecycleState.Sunset, FixedNow);
        if (state >= ContractLifecycleState.Retired)
            version.TransitionTo(ContractLifecycleState.Retired, FixedNow);

        return version;
    }

    [Fact]
    public async Task Should_Return_Ready_When_No_Checks_Configured()
    {
        var (repo, config, dt) = CreateMocks();
        var version = CreateVersion();
        repo.GetDetailAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>()).Returns(version);

        config.ResolveEffectiveValueAsync(Arg.Any<string>(), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);

        var sut = new ValidateFeature.Handler(repo, config, dt);
        var result = await sut.Handle(new ValidateFeature.Query(version.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsReadyToPublish.Should().BeTrue();
        result.Value.TotalChecks.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_Lint_Check_When_Version_Has_Violations()
    {
        var (repo, config, dt) = CreateMocks();
        var version = CreateVersion();

        // Add a rule violation
        version.AddRuleViolation(ContractRuleViolation.Create(
            version.Id, null, "naming-convention", "Error",
            "Paths must use kebab-case", "/paths/getUser",
            FixedNow));

        repo.GetDetailAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>()).Returns(version);

        config.ResolveEffectiveValueAsync(
            "catalog.contract.validation.block_on_lint_errors", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("catalog.contract.validation.block_on_lint_errors", "true"));
        config.ResolveEffectiveValueAsync(
            Arg.Is<string>(k => k != "catalog.contract.validation.block_on_lint_errors"), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);

        var sut = new ValidateFeature.Handler(repo, config, dt);
        var result = await sut.Handle(new ValidateFeature.Query(version.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsReadyToPublish.Should().BeFalse();
        result.Value.FailedChecks.Should().Be(1);
        result.Value.Checks.Should().ContainSingle(c => c.CheckId == "lint_errors" && !c.Passed);
    }

    [Fact]
    public async Task Should_Pass_Lint_Check_When_Version_Has_No_Violations()
    {
        var (repo, config, dt) = CreateMocks();
        var version = CreateVersion();

        repo.GetDetailAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>()).Returns(version);

        config.ResolveEffectiveValueAsync(
            "catalog.contract.validation.block_on_lint_errors", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("catalog.contract.validation.block_on_lint_errors", "true"));
        config.ResolveEffectiveValueAsync(
            Arg.Is<string>(k => k != "catalog.contract.validation.block_on_lint_errors"), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);

        var sut = new ValidateFeature.Handler(repo, config, dt);
        var result = await sut.Handle(new ValidateFeature.Query(version.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsReadyToPublish.Should().BeTrue();
        result.Value.Checks.Should().ContainSingle(c => c.CheckId == "lint_errors" && c.Passed);
    }

    [Fact]
    public async Task Should_Fail_Examples_Check_When_Spec_Has_No_Examples()
    {
        var (repo, config, dt) = CreateMocks();
        var version = CreateVersion(specContent: """{"openapi":"3.0.0","paths":{"/users":{"get":{"responses":{"200":{"description":"OK"}}}}}}""");

        repo.GetDetailAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>()).Returns(version);

        config.ResolveEffectiveValueAsync(
            "catalog.contract.publication.require_examples", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("catalog.contract.publication.require_examples", "true"));
        config.ResolveEffectiveValueAsync(
            Arg.Is<string>(k => k != "catalog.contract.publication.require_examples"), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);

        var sut = new ValidateFeature.Handler(repo, config, dt);
        var result = await sut.Handle(new ValidateFeature.Query(version.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsReadyToPublish.Should().BeFalse();
        result.Value.Checks.Should().ContainSingle(c => c.CheckId == "require_examples" && !c.Passed);
    }

    [Fact]
    public async Task Should_Pass_Examples_Check_When_Spec_Contains_Examples()
    {
        var (repo, config, dt) = CreateMocks();
        var version = CreateVersion(specContent: """{"openapi":"3.0.0","paths":{"/users":{"get":{"responses":{"200":{"description":"OK","content":{"application/json":{"example":{"id":1}}}}}}}}}""");

        repo.GetDetailAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>()).Returns(version);

        config.ResolveEffectiveValueAsync(
            "catalog.contract.publication.require_examples", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("catalog.contract.publication.require_examples", "true"));
        config.ResolveEffectiveValueAsync(
            Arg.Is<string>(k => k != "catalog.contract.publication.require_examples"), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);

        var sut = new ValidateFeature.Handler(repo, config, dt);
        var result = await sut.Handle(new ValidateFeature.Query(version.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsReadyToPublish.Should().BeTrue();
        result.Value.Checks.Should().ContainSingle(c => c.CheckId == "require_examples" && c.Passed);
    }

    [Fact]
    public async Task Should_Fail_Approval_Check_When_Version_Is_Draft()
    {
        var (repo, config, dt) = CreateMocks();
        var version = CreateVersion(state: ContractLifecycleState.Draft);

        repo.GetDetailAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>()).Returns(version);

        config.ResolveEffectiveValueAsync(
            "catalog.contract.creation.approval_required", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("catalog.contract.creation.approval_required", "true"));
        config.ResolveEffectiveValueAsync(
            Arg.Is<string>(k => k != "catalog.contract.creation.approval_required"), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);

        var sut = new ValidateFeature.Handler(repo, config, dt);
        var result = await sut.Handle(new ValidateFeature.Query(version.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsReadyToPublish.Should().BeFalse();
        result.Value.Checks.Should().ContainSingle(c => c.CheckId == "approval_required" && !c.Passed);
    }

    [Fact]
    public async Task Should_Pass_Approval_Check_When_Version_Is_Approved()
    {
        var (repo, config, dt) = CreateMocks();
        var version = CreateVersion(state: ContractLifecycleState.Approved);

        repo.GetDetailAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>()).Returns(version);

        config.ResolveEffectiveValueAsync(
            "catalog.contract.creation.approval_required", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("catalog.contract.creation.approval_required", "true"));
        config.ResolveEffectiveValueAsync(
            Arg.Is<string>(k => k != "catalog.contract.creation.approval_required"), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);

        var sut = new ValidateFeature.Handler(repo, config, dt);
        var result = await sut.Handle(new ValidateFeature.Query(version.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsReadyToPublish.Should().BeTrue();
        result.Value.Checks.Should().ContainSingle(c => c.CheckId == "approval_required" && c.Passed);
    }

    [Fact]
    public async Task Should_Return_Error_When_ContractVersion_Not_Found()
    {
        var (repo, config, dt) = CreateMocks();
        repo.GetDetailAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>()).Returns((ContractVersion?)null);

        var sut = new ValidateFeature.Handler(repo, config, dt);
        var result = await sut.Handle(new ValidateFeature.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Evaluate_All_Configured_Checks_And_Report_Mixed_Results()
    {
        var (repo, config, dt) = CreateMocks();
        // Draft with examples but with lint violations
        var version = CreateVersion(specContent: """{"openapi":"3.0.0","paths":{"/users":{"get":{"responses":{"200":{"description":"OK","content":{"application/json":{"example":{"id":1}}}}}}}}}""");
        version.AddRuleViolation(ContractRuleViolation.Create(
            version.Id, null, "required-description", "Error",
            "Missing description", "/paths/~1users/get",
            FixedNow));

        repo.GetDetailAsync(Arg.Any<ContractVersionId>(), Arg.Any<CancellationToken>()).Returns(version);

        // Enable all 3 checks
        config.ResolveEffectiveValueAsync(
            "catalog.contract.validation.block_on_lint_errors", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("catalog.contract.validation.block_on_lint_errors", "true"));
        config.ResolveEffectiveValueAsync(
            "catalog.contract.publication.require_examples", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("catalog.contract.publication.require_examples", "true"));
        config.ResolveEffectiveValueAsync(
            "catalog.contract.creation.approval_required", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("catalog.contract.creation.approval_required", "true"));

        var sut = new ValidateFeature.Handler(repo, config, dt);
        var result = await sut.Handle(new ValidateFeature.Query(version.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsReadyToPublish.Should().BeFalse();
        result.Value.TotalChecks.Should().Be(3);
        result.Value.PassedChecks.Should().Be(1); // only examples passed
        result.Value.FailedChecks.Should().Be(2); // lint + approval failed
        result.Value.Checks.Should().Contain(c => c.CheckId == "lint_errors" && !c.Passed);
        result.Value.Checks.Should().Contain(c => c.CheckId == "require_examples" && c.Passed);
        result.Value.Checks.Should().Contain(c => c.CheckId == "approval_required" && !c.Passed);
    }
}
