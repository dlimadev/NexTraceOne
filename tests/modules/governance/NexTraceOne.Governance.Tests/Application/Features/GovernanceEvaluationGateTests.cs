using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.Governance.Application.Features.EvaluateChangeAdvisoryBoard;
using NexTraceOne.Governance.Application.Features.EvaluateComplianceRemediationGate;
using NexTraceOne.Governance.Application.Features.EvaluateErrorBudgetGate;
using NexTraceOne.Governance.Application.Features.EvaluateFinOpsBudgetGate;
using NexTraceOne.Governance.Application.Features.EvaluateFourEyesPrinciple;
using NexTraceOne.Governance.Application.Features.EvaluateReleaseBudgetGate;
using NexTraceOne.Integrations.Domain;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes unitários para os evaluation gates de Governance:
/// EvaluateChangeAdvisoryBoard, EvaluateComplianceRemediationGate,
/// EvaluateErrorBudgetGate, EvaluateFinOpsBudgetGate,
/// EvaluateFourEyesPrinciple, EvaluateReleaseBudgetGate.
/// Cobre cenários de gate desativado, dentro do limiar, acima do limiar e bloqueio.
/// </summary>
public sealed class GovernanceEvaluationGateTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 19, 12, 0, 0, TimeSpan.Zero);

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static IConfigurationResolutionService CreateConfigService(
        Dictionary<string, string?> values = null!)
    {
        var svc = Substitute.For<IConfigurationResolutionService>();
        svc.ResolveEffectiveValueAsync(
                Arg.Any<string>(),
                Arg.Any<ConfigurationScope>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);

        if (values != null)
        {
            foreach (var (key, value) in values)
            {
                var dto = value is null ? null : new EffectiveConfigurationDto(
                    key, value, "tenant", null, false, false, key, "string", false, 1);
                svc.ResolveEffectiveValueAsync(
                        key,
                        Arg.Any<ConfigurationScope>(),
                        Arg.Any<string?>(),
                        Arg.Any<CancellationToken>())
                    .Returns(dto);
            }
        }

        return svc;
    }

    // ── EvaluateChangeAdvisoryBoard ─────────────────────────────────────────

    [Fact]
    public async Task EvaluateChangeAdvisoryBoard_CabDisabled_NoCabRequired()
    {
        var config = CreateConfigService(new Dictionary<string, string?>
        {
            ["governance.change_advisory_board.enabled"] = "false"
        });

        var handler = new EvaluateChangeAdvisoryBoard.Handler(config, CreateClock());
        var result = await handler.Handle(
            new EvaluateChangeAdvisoryBoard.Query("my-service", "Production", "High", "Large"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CabRequired.Should().BeFalse();
        result.Value.IsApproved.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateChangeAdvisoryBoard_CabEnabled_CabRequired()
    {
        var config = CreateConfigService(new Dictionary<string, string?>
        {
            ["governance.change_advisory_board.enabled"] = "true",
            ["governance.change_advisory_board.members"] = "[\"cto@example.com\",\"ciso@example.com\"]",
            ["governance.change_advisory_board.trigger_conditions"] = "[\"Production\",\"High\"]"
        });

        var handler = new EvaluateChangeAdvisoryBoard.Handler(config, CreateClock());
        var result = await handler.Handle(
            new EvaluateChangeAdvisoryBoard.Query("api-service", "Production", "High", "Large"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CabRequired.Should().BeTrue();
        result.Value.IsApproved.Should().BeFalse();
        result.Value.EvaluatedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task EvaluateChangeAdvisoryBoard_NoConfig_DefaultsToNoCab()
    {
        var config = CreateConfigService();

        var handler = new EvaluateChangeAdvisoryBoard.Handler(config, CreateClock());
        var result = await handler.Handle(
            new EvaluateChangeAdvisoryBoard.Query("srv", "Development", "Low", "Small"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CabRequired.Should().BeFalse();
    }

    // ── EvaluateComplianceRemediationGate ────────────────────────────────────

    [Fact]
    public async Task EvaluateComplianceRemediationGate_AutoRemediationDisabled_NotEligible()
    {
        var config = CreateConfigService(new Dictionary<string, string?>
        {
            ["governance.compliance.auto_remediation.enabled"] = "false",
        });

        var handler = new EvaluateComplianceRemediationGate.Handler(config, CreateClock());
        var result = await handler.Handle(
            new EvaluateComplianceRemediationGate.Query("missing-encryption", "payment-service", "Critical"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AutoRemediationEnabled.Should().BeFalse();
        result.Value.IsEligibleForAutoRemediation.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateComplianceRemediationGate_AutoRemediationEnabled_ReturnsEligibility()
    {
        var config = CreateConfigService(new Dictionary<string, string?>
        {
            ["governance.compliance.auto_remediation.enabled"] = "true",
            ["governance.compliance.framework"] = "[\"SOC2TypeII\",\"GDPR\"]"
        });

        var handler = new EvaluateComplianceRemediationGate.Handler(config, CreateClock());
        var result = await handler.Handle(
            new EvaluateComplianceRemediationGate.Query("tls-disabled", "api-service", "High"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AutoRemediationEnabled.Should().BeTrue();
    }

    // ── EvaluateErrorBudgetGate ─────────────────────────────────────────────

    [Fact]
    public async Task EvaluateErrorBudgetGate_AutoBlockDisabled_NotBlocked()
    {
        var config = CreateConfigService(new Dictionary<string, string?>
        {
            ["reliability.error_budget.auto_block_deploys"] = "false"
        });

        var handler = new EvaluateErrorBudgetGate.Handler(config, CreateClock());
        var result = await handler.Handle(
            new EvaluateErrorBudgetGate.Query("checkout-service", "Production", ErrorBudgetRemainingPct: 5m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsBlocked.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateErrorBudgetGate_BudgetAboveThreshold_NotBlocked()
    {
        var config = CreateConfigService(new Dictionary<string, string?>
        {
            ["reliability.error_budget.auto_block_deploys"] = "true",
            ["reliability.error_budget.block_threshold_pct"] = "10"
        });

        var handler = new EvaluateErrorBudgetGate.Handler(config, CreateClock());
        var result = await handler.Handle(
            new EvaluateErrorBudgetGate.Query("order-service", "Production", ErrorBudgetRemainingPct: 50m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsBlocked.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateErrorBudgetGate_BudgetBelowThreshold_Blocked()
    {
        var config = CreateConfigService(new Dictionary<string, string?>
        {
            ["reliability.error_budget.auto_block_deploys"] = "true",
            ["reliability.error_budget.block_threshold_pct"] = "10"
        });

        var handler = new EvaluateErrorBudgetGate.Handler(config, CreateClock());
        var result = await handler.Handle(
            new EvaluateErrorBudgetGate.Query("order-service", "Production", ErrorBudgetRemainingPct: 3m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsBlocked.Should().BeTrue();
        result.Value.Reason.Should().NotBeNullOrWhiteSpace();
        result.Value.EvaluatedAt.Should().Be(FixedNow);
    }

    // ── EvaluateFinOpsBudgetGate ─────────────────────────────────────────────

    [Fact]
    public async Task EvaluateFinOpsBudgetGate_NoConfig_UsesDefaultThreshold()
    {
        var config = CreateConfigService();

        var handler = new EvaluateFinOpsBudgetGate.Handler(config, CreateClock());
        var result = await handler.Handle(
            new EvaluateFinOpsBudgetGate.Query("analytics-service", "platform-team", CurrentSpendPct: 50m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("analytics-service");
        result.Value.TeamName.Should().Be("platform-team");
    }

    [Fact]
    public async Task EvaluateFinOpsBudgetGate_BudgetBelowThreshold_GatePassed()
    {
        var config = CreateConfigService(new Dictionary<string, string?>
        {
            ["finops.budget_alert_threshold"] = "80"
        });

        var handler = new EvaluateFinOpsBudgetGate.Handler(config, CreateClock());
        var result = await handler.Handle(
            new EvaluateFinOpsBudgetGate.Query("catalog-service", "catalog-team", CurrentSpendPct: 60m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsOverThreshold.Should().BeFalse();
        result.Value.EvaluatedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task EvaluateFinOpsBudgetGate_BudgetAboveThreshold_GateFailed()
    {
        var config = CreateConfigService(new Dictionary<string, string?>
        {
            ["finops.budget_alert_threshold"] = "80"
        });

        var handler = new EvaluateFinOpsBudgetGate.Handler(config, CreateClock());
        var result = await handler.Handle(
            new EvaluateFinOpsBudgetGate.Query("heavy-service", "ops-team", CurrentSpendPct: 95m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsOverThreshold.Should().BeTrue();
    }

    // ── EvaluateFourEyesPrinciple ────────────────────────────────────────────

    [Fact]
    public async Task EvaluateFourEyesPrinciple_Disabled_NoRequirement()
    {
        var config = CreateConfigService(new Dictionary<string, string?>
        {
            ["governance.four_eyes_principle.enabled"] = "false"
        });

        var handler = new EvaluateFourEyesPrinciple.Handler(config, CreateClock());
        var result = await handler.Handle(
            new EvaluateFourEyesPrinciple.Query("production-deploy", "user-1", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FourEyesRequired.Should().BeFalse();
        result.Value.IsCompliant.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateFourEyesPrinciple_EnabledNoApprover_RequiresSecondApprover()
    {
        var config = CreateConfigService(new Dictionary<string, string?>
        {
            ["governance.four_eyes_principle.enabled"] = "true",
            ["governance.four_eyes_principle.actions"] = "[\"production-deploy\",\"config-change\"]"
        });

        var handler = new EvaluateFourEyesPrinciple.Handler(config, CreateClock());
        var result = await handler.Handle(
            new EvaluateFourEyesPrinciple.Query("production-deploy", "user-1", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FourEyesRequired.Should().BeTrue();
        result.Value.RequiresSecondApprover.Should().BeTrue();
        result.Value.IsCompliant.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateFourEyesPrinciple_EnabledWithApprover_Compliant()
    {
        var config = CreateConfigService(new Dictionary<string, string?>
        {
            ["governance.four_eyes_principle.enabled"] = "true",
            ["governance.four_eyes_principle.actions"] = "[\"production-deploy\"]"
        });

        var handler = new EvaluateFourEyesPrinciple.Handler(config, CreateClock());
        var result = await handler.Handle(
            new EvaluateFourEyesPrinciple.Query("production-deploy", "user-1", "user-2"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FourEyesRequired.Should().BeTrue();
        result.Value.IsCompliant.Should().BeTrue();
    }

    // ── EvaluateReleaseBudgetGate ────────────────────────────────────────────

    [Fact]
    public async Task EvaluateReleaseBudgetGate_GateDisabled_AllowsRelease()
    {
        var config = CreateConfigService(new Dictionary<string, string?>
        {
            ["finops.release.budget_gate.enabled"] = "false"
        });

        var releaseId = Guid.NewGuid();
        var handler = new EvaluateReleaseBudgetGate.Handler(config, CreateClock());
        var result = await handler.Handle(
            new EvaluateReleaseBudgetGate.Query(releaseId, "catalog-service", "Production", 100m, 90m, 7),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(EvaluateReleaseBudgetGate.BudgetGateAction.Allow);
    }

    [Fact]
    public async Task EvaluateReleaseBudgetGate_WithinBudget_AllowsRelease()
    {
        var config = CreateConfigService(new Dictionary<string, string?>
        {
            ["finops.release.budget_gate.enabled"] = "true",
            ["finops.budget.alert_thresholds"] = "80",
        });

        var releaseId = Guid.NewGuid();
        var handler = new EvaluateReleaseBudgetGate.Handler(config, CreateClock());
        var result = await handler.Handle(
            new EvaluateReleaseBudgetGate.Query(releaseId, "catalog-service", "Production",
                ActualCostPerDay: 100m, BaselineCostPerDay: 95m, MeasurementDays: 7),
            CancellationToken.None);

        // costDeltaPct = (5/95)*100 ≈ 5.3% < 80% threshold → Allow
        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(EvaluateReleaseBudgetGate.BudgetGateAction.Allow);
    }

    [Fact]
    public async Task EvaluateReleaseBudgetGate_ExceedsBudget_BlocksRelease()
    {
        var config = CreateConfigService(new Dictionary<string, string?>
        {
            ["finops.release.budget_gate.enabled"] = "true",
            ["finops.budget.alert_thresholds"] = "20",
            ["finops.release.budget_gate.block_on_exceed"] = "true",
            ["finops.release.budget_gate.require_approval"] = "false"
        });

        var releaseId = Guid.NewGuid();
        var handler = new EvaluateReleaseBudgetGate.Handler(config, CreateClock());
        var result = await handler.Handle(
            new EvaluateReleaseBudgetGate.Query(releaseId, "heavy-service", "Production",
                ActualCostPerDay: 250m, BaselineCostPerDay: 90m, MeasurementDays: 7),
            CancellationToken.None);

        // costDeltaPct ≈ 177.78% > 20% threshold → Block
        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(EvaluateReleaseBudgetGate.BudgetGateAction.Block);
        result.Value.EvaluatedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task EvaluateReleaseBudgetGate_ExceedsRequiresApproval_RequireApprovalAction()
    {
        var config = CreateConfigService(new Dictionary<string, string?>
        {
            ["finops.release.budget_gate.enabled"] = "true",
            ["finops.budget.alert_thresholds"] = "20",
            ["finops.release.budget_gate.block_on_exceed"] = "true",
            ["finops.release.budget_gate.require_approval"] = "true"
        });

        var releaseId = Guid.NewGuid();
        var handler = new EvaluateReleaseBudgetGate.Handler(config, CreateClock());
        var result = await handler.Handle(
            new EvaluateReleaseBudgetGate.Query(releaseId, "large-service", "Production",
                ActualCostPerDay: 300m, BaselineCostPerDay: 90m, MeasurementDays: 7),
            CancellationToken.None);

        // costDeltaPct ≈ 233% > 20% threshold → RequireApproval
        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(EvaluateReleaseBudgetGate.BudgetGateAction.RequireApproval);
    }
}
