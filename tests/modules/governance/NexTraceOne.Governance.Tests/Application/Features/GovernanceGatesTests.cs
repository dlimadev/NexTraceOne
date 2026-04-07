using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;

using FourEyesFeature = NexTraceOne.Governance.Application.Features.EvaluateFourEyesPrinciple.EvaluateFourEyesPrinciple;
using CabFeature = NexTraceOne.Governance.Application.Features.EvaluateChangeAdvisoryBoard.EvaluateChangeAdvisoryBoard;
using ErrorBudgetFeature = NexTraceOne.Governance.Application.Features.EvaluateErrorBudgetGate.EvaluateErrorBudgetGate;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes para Four Eyes Principle e Change Advisory Board — gates de governança avançada.
/// </summary>
public sealed class GovernanceGatesTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static EffectiveConfigurationDto CreateConfig(string key, string? value) =>
        new(key, value, "Tenant", null, false, false, key, "string", false, 1);

    private static (IConfigurationResolutionService config, IDateTimeProvider dt) CreateMocks()
    {
        var config = Substitute.For<IConfigurationResolutionService>();
        var dt = Substitute.For<IDateTimeProvider>();
        dt.UtcNow.Returns(FixedNow);
        return (config, dt);
    }

    // ── Four Eyes Principle ─────────────────────────────────

    [Fact]
    public async Task FourEyes_Should_Not_Require_When_Disabled()
    {
        var (config, dt) = CreateMocks();
        config.ResolveEffectiveValueAsync("governance.four_eyes_principle.enabled", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("governance.four_eyes_principle.enabled", "false"));

        var sut = new FourEyesFeature.Handler(config, dt);
        var result = await sut.Handle(new FourEyesFeature.Query("production_deploy", "admin@co.com", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FourEyesRequired.Should().BeFalse();
        result.Value.IsCompliant.Should().BeTrue();
    }

    [Fact]
    public async Task FourEyes_Should_Not_Require_When_Action_Not_Covered()
    {
        var (config, dt) = CreateMocks();
        config.ResolveEffectiveValueAsync("governance.four_eyes_principle.enabled", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("governance.four_eyes_principle.enabled", "true"));
        config.ResolveEffectiveValueAsync("governance.four_eyes_principle.actions", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("governance.four_eyes_principle.actions", """["production_deploy"]"""));

        var sut = new FourEyesFeature.Handler(config, dt);
        var result = await sut.Handle(new FourEyesFeature.Query("config_change", "admin@co.com", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FourEyesRequired.Should().BeFalse();
        result.Value.IsCompliant.Should().BeTrue();
    }

    [Fact]
    public async Task FourEyes_Should_Fail_When_No_Second_Approver()
    {
        var (config, dt) = CreateMocks();
        config.ResolveEffectiveValueAsync("governance.four_eyes_principle.enabled", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("governance.four_eyes_principle.enabled", "true"));
        config.ResolveEffectiveValueAsync("governance.four_eyes_principle.actions", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("governance.four_eyes_principle.actions", """["production_deploy"]"""));

        var sut = new FourEyesFeature.Handler(config, dt);
        var result = await sut.Handle(new FourEyesFeature.Query("production_deploy", "admin@co.com", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FourEyesRequired.Should().BeTrue();
        result.Value.IsCompliant.Should().BeFalse();
        result.Value.RequiresSecondApprover.Should().BeTrue();
    }

    [Fact]
    public async Task FourEyes_Should_Fail_When_Same_Person()
    {
        var (config, dt) = CreateMocks();
        config.ResolveEffectiveValueAsync("governance.four_eyes_principle.enabled", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("governance.four_eyes_principle.enabled", "true"));
        config.ResolveEffectiveValueAsync("governance.four_eyes_principle.actions", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("governance.four_eyes_principle.actions", """["production_deploy"]"""));

        var sut = new FourEyesFeature.Handler(config, dt);
        var result = await sut.Handle(new FourEyesFeature.Query("production_deploy", "admin@co.com", "admin@co.com"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FourEyesRequired.Should().BeTrue();
        result.Value.IsCompliant.Should().BeFalse();
    }

    [Fact]
    public async Task FourEyes_Should_Pass_When_Different_Approver()
    {
        var (config, dt) = CreateMocks();
        config.ResolveEffectiveValueAsync("governance.four_eyes_principle.enabled", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("governance.four_eyes_principle.enabled", "true"));
        config.ResolveEffectiveValueAsync("governance.four_eyes_principle.actions", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("governance.four_eyes_principle.actions", """["production_deploy"]"""));

        var sut = new FourEyesFeature.Handler(config, dt);
        var result = await sut.Handle(new FourEyesFeature.Query("production_deploy", "admin@co.com", "lead@co.com"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FourEyesRequired.Should().BeTrue();
        result.Value.IsCompliant.Should().BeTrue();
        result.Value.RequiresSecondApprover.Should().BeFalse();
    }

    // ── Change Advisory Board ───────────────────────────────

    [Fact]
    public async Task CAB_Should_Not_Require_When_Disabled()
    {
        var (config, dt) = CreateMocks();
        config.ResolveEffectiveValueAsync("governance.change_advisory_board.enabled", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("governance.change_advisory_board.enabled", "false"));

        var sut = new CabFeature.Handler(config, dt);
        var result = await sut.Handle(new CabFeature.Query("order-service", "production", "Critical", "High"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CabRequired.Should().BeFalse();
        result.Value.IsApproved.Should().BeTrue();
    }

    [Fact]
    public async Task CAB_Should_Require_For_High_Criticality_Production()
    {
        var (config, dt) = CreateMocks();
        config.ResolveEffectiveValueAsync("governance.change_advisory_board.enabled", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("governance.change_advisory_board.enabled", "true"));
        config.ResolveEffectiveValueAsync("governance.change_advisory_board.trigger_conditions", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("governance.change_advisory_board.trigger_conditions",
                """{"min_criticality": "High", "min_blast_radius": "Medium", "environment": ["production"]}"""));
        config.ResolveEffectiveValueAsync("governance.change_advisory_board.members", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("governance.change_advisory_board.members",
                """[{"type":"role","value":"Architect"}]"""));

        var sut = new CabFeature.Handler(config, dt);
        var result = await sut.Handle(new CabFeature.Query("order-service", "production", "Critical", "High"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CabRequired.Should().BeTrue();
        result.Value.IsApproved.Should().BeFalse();
        result.Value.TriggerConditions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CAB_Should_Not_Require_For_Low_Criticality()
    {
        var (config, dt) = CreateMocks();
        config.ResolveEffectiveValueAsync("governance.change_advisory_board.enabled", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("governance.change_advisory_board.enabled", "true"));
        config.ResolveEffectiveValueAsync("governance.change_advisory_board.trigger_conditions", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("governance.change_advisory_board.trigger_conditions",
                """{"min_criticality": "High", "min_blast_radius": "Medium", "environment": ["production"]}"""));

        var sut = new CabFeature.Handler(config, dt);
        var result = await sut.Handle(new CabFeature.Query("order-service", "staging", "Low", "None"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CabRequired.Should().BeFalse();
        result.Value.IsApproved.Should().BeTrue();
    }

    // ── Error Budget Gate ───────────────────────────────────

    [Fact]
    public async Task ErrorBudget_Should_Not_Block_When_Disabled()
    {
        var (config, dt) = CreateMocks();
        config.ResolveEffectiveValueAsync("reliability.error_budget.auto_block_deploys", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("reliability.error_budget.auto_block_deploys", "false"));

        var sut = new ErrorBudgetFeature.Handler(config, dt);
        var result = await sut.Handle(new ErrorBudgetFeature.Query("order-service", "production", 5m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsBlocked.Should().BeFalse();
    }

    [Fact]
    public async Task ErrorBudget_Should_Block_When_Below_Threshold()
    {
        var (config, dt) = CreateMocks();
        config.ResolveEffectiveValueAsync("reliability.error_budget.auto_block_deploys", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("reliability.error_budget.auto_block_deploys", "true"));
        config.ResolveEffectiveValueAsync("reliability.error_budget.block_threshold_pct", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("reliability.error_budget.block_threshold_pct", "10"));

        var sut = new ErrorBudgetFeature.Handler(config, dt);
        var result = await sut.Handle(new ErrorBudgetFeature.Query("order-service", "production", 5m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsBlocked.Should().BeTrue();
        result.Value.BlockThresholdPct.Should().Be(10m);
    }

    [Fact]
    public async Task ErrorBudget_Should_Allow_When_Above_Threshold()
    {
        var (config, dt) = CreateMocks();
        config.ResolveEffectiveValueAsync("reliability.error_budget.auto_block_deploys", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("reliability.error_budget.auto_block_deploys", "true"));
        config.ResolveEffectiveValueAsync("reliability.error_budget.block_threshold_pct", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("reliability.error_budget.block_threshold_pct", "10"));

        var sut = new ErrorBudgetFeature.Handler(config, dt);
        var result = await sut.Handle(new ErrorBudgetFeature.Query("order-service", "production", 25m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsBlocked.Should().BeFalse();
    }
}
