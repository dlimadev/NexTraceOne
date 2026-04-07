using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;

using FinOpsFeature = NexTraceOne.Governance.Application.Features.EvaluateFinOpsBudgetGate.EvaluateFinOpsBudgetGate;
using ComplianceFeature = NexTraceOne.Governance.Application.Features.EvaluateComplianceRemediationGate.EvaluateComplianceRemediationGate;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes de Phase 4 — FinOps budget gate e compliance auto-remediation gate.
/// </summary>
public sealed class FinOpsComplianceGatesTests
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

    // ── FinOps Budget Gate ──────────────────────────────────

    [Fact]
    public async Task FinOps_Should_Not_Alert_Under_Threshold()
    {
        var (config, dt) = CreateMocks();
        config.ResolveEffectiveValueAsync("finops.budget_alert_threshold", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("finops.budget_alert_threshold", "80"));
        config.ResolveEffectiveValueAsync("finops.chargeback.enabled", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("finops.chargeback.enabled", "false"));
        config.ResolveEffectiveValueAsync("finops.budget.by_service", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("finops.budget.by_service", """{"default":{"monthlyBudget":2000}}"""));

        var sut = new FinOpsFeature.Handler(config, dt);
        var result = await sut.Handle(new FinOpsFeature.Query("order-service", "platform-team", 50m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsOverThreshold.Should().BeFalse();
        result.Value.IsOverBudget.Should().BeFalse();
        result.Value.Alerts.Should().BeEmpty();
    }

    [Fact]
    public async Task FinOps_Should_Alert_Over_Threshold()
    {
        var (config, dt) = CreateMocks();
        config.ResolveEffectiveValueAsync("finops.budget_alert_threshold", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("finops.budget_alert_threshold", "80"));
        config.ResolveEffectiveValueAsync("finops.chargeback.enabled", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("finops.chargeback.enabled", "false"));
        config.ResolveEffectiveValueAsync("finops.budget.by_service", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("finops.budget.by_service", """{"default":{"monthlyBudget":2000}}"""));

        var sut = new FinOpsFeature.Handler(config, dt);
        var result = await sut.Handle(new FinOpsFeature.Query("order-service", "platform-team", 90m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsOverThreshold.Should().BeTrue();
        result.Value.IsOverBudget.Should().BeFalse();
        result.Value.Alerts.Should().NotBeEmpty();
    }

    [Fact]
    public async Task FinOps_Should_Flag_Over_Budget()
    {
        var (config, dt) = CreateMocks();
        config.ResolveEffectiveValueAsync("finops.budget_alert_threshold", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("finops.budget_alert_threshold", "80"));
        config.ResolveEffectiveValueAsync("finops.chargeback.enabled", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("finops.chargeback.enabled", "true"));
        config.ResolveEffectiveValueAsync("finops.budget.by_service", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("finops.budget.by_service", """{"default":{"monthlyBudget":2000}}"""));

        var sut = new FinOpsFeature.Handler(config, dt);
        var result = await sut.Handle(new FinOpsFeature.Query("order-service", "platform-team", 120m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsOverBudget.Should().BeTrue();
        result.Value.ChargebackEnabled.Should().BeTrue();
        result.Value.Alerts.Should().HaveCountGreaterThan(1);
    }

    // ── Compliance Auto-Remediation Gate ────────────────────

    [Fact]
    public async Task Compliance_Should_Not_AutoRemediate_When_Disabled()
    {
        var (config, dt) = CreateMocks();
        config.ResolveEffectiveValueAsync("governance.compliance.auto_remediation.enabled", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("governance.compliance.auto_remediation.enabled", "false"));
        config.ResolveEffectiveValueAsync("governance.compliance.framework", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("governance.compliance.framework", """["internal"]"""));

        var sut = new ComplianceFeature.Handler(config, dt);
        var result = await sut.Handle(new ComplianceFeature.Query("missing-owner", "order-service", "Low"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AutoRemediationEnabled.Should().BeFalse();
        result.Value.IsEligibleForAutoRemediation.Should().BeFalse();
    }

    [Fact]
    public async Task Compliance_Should_AutoRemediate_Low_Severity()
    {
        var (config, dt) = CreateMocks();
        config.ResolveEffectiveValueAsync("governance.compliance.auto_remediation.enabled", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("governance.compliance.auto_remediation.enabled", "true"));
        config.ResolveEffectiveValueAsync("governance.compliance.framework", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("governance.compliance.framework", """["internal","SOC2"]"""));

        var sut = new ComplianceFeature.Handler(config, dt);
        var result = await sut.Handle(new ComplianceFeature.Query("missing-docs", "order-service", "Low"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AutoRemediationEnabled.Should().BeTrue();
        result.Value.IsEligibleForAutoRemediation.Should().BeTrue();
        result.Value.ActiveFrameworks.Should().Contain("SOC2");
    }

    [Fact]
    public async Task Compliance_Should_Not_AutoRemediate_High_Severity()
    {
        var (config, dt) = CreateMocks();
        config.ResolveEffectiveValueAsync("governance.compliance.auto_remediation.enabled", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("governance.compliance.auto_remediation.enabled", "true"));
        config.ResolveEffectiveValueAsync("governance.compliance.framework", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateConfig("governance.compliance.framework", """["internal"]"""));

        var sut = new ComplianceFeature.Handler(config, dt);
        var result = await sut.Handle(new ComplianceFeature.Query("security-vulnerability", "order-service", "High"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AutoRemediationEnabled.Should().BeTrue();
        result.Value.IsEligibleForAutoRemediation.Should().BeFalse();
    }
}
