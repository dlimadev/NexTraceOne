using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;
using NexTraceOne.Configuration.Domain.Enums;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.CreateFinOpsBudgetApproval;
using NexTraceOne.Governance.Application.Features.GetFinOpsConfiguration;
using NexTraceOne.Governance.Application.Features.ListFinOpsBudgetApprovals;
using NexTraceOne.Governance.Application.Features.ResolveFinOpsBudgetApproval;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

using GetFinOpsConfigFeature = NexTraceOne.Governance.Application.Features.GetFinOpsConfiguration.GetFinOpsConfiguration;
using EvaluateGateFeature = NexTraceOne.Governance.Application.Features.EvaluateReleaseBudgetGate.EvaluateReleaseBudgetGate;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes de unidade para as novas features de configuração e aprovação de orçamento FinOps.
/// Cobre: GetFinOpsConfiguration, EvaluateReleaseBudgetGate, CreateFinOpsBudgetApproval,
/// ResolveFinOpsBudgetApproval, ListFinOpsBudgetApprovals.
/// </summary>
public sealed class FinOpsConfigAndApprovalTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 18, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid SampleReleaseId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    private static EffectiveConfigurationDto Cfg(string key, string? value) =>
        new(key, value, "Tenant", null, false, false, key, "string", false, 1);

    private static (IConfigurationResolutionService cfg, IDateTimeProvider dt) CreateMocks()
    {
        var cfg = Substitute.For<IConfigurationResolutionService>();
        var dt = Substitute.For<IDateTimeProvider>();
        dt.UtcNow.Returns(FixedNow);
        return (cfg, dt);
    }

    // ── GetFinOpsConfiguration ────────────────────────────────────────

    [Fact]
    public async Task GetFinOpsConfig_Returns_Defaults_When_No_Config()
    {
        var (cfg, dt) = CreateMocks();
        cfg.ResolveEffectiveValueAsync(Arg.Any<string>(), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);

        var sut = new GetFinOpsConfigFeature.Handler(cfg, dt);
        var result = await sut.Handle(new GetFinOpsConfigFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Currency.Should().Be("USD");
        result.Value.BudgetGateEnabled.Should().BeFalse();
        result.Value.BlockOnExceed.Should().BeTrue();
        result.Value.RequireApproval.Should().BeTrue();
        result.Value.AlertThresholdPct.Should().Be(80m);
        result.Value.Approvers.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFinOpsConfig_Returns_Configured_Currency()
    {
        var (cfg, dt) = CreateMocks();
        cfg.ResolveEffectiveValueAsync(Arg.Any<string>(), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);
        cfg.ResolveEffectiveValueAsync("finops.budget.default_currency", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Cfg("finops.budget.default_currency", "eur"));

        var sut = new GetFinOpsConfigFeature.Handler(cfg, dt);
        var result = await sut.Handle(new GetFinOpsConfigFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Currency.Should().Be("EUR");
    }

    [Fact]
    public async Task GetFinOpsConfig_Returns_Gate_Settings()
    {
        var (cfg, dt) = CreateMocks();
        cfg.ResolveEffectiveValueAsync(Arg.Any<string>(), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);
        cfg.ResolveEffectiveValueAsync("finops.budget.default_currency", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Cfg("finops.budget.default_currency", "BRL"));
        cfg.ResolveEffectiveValueAsync("finops.release.budget_gate.enabled", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Cfg("finops.release.budget_gate.enabled", "true"));
        cfg.ResolveEffectiveValueAsync("finops.release.budget_gate.block_on_exceed", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Cfg("finops.release.budget_gate.block_on_exceed", "false"));
        cfg.ResolveEffectiveValueAsync("finops.release.budget_gate.require_approval", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Cfg("finops.release.budget_gate.require_approval", "true"));
        cfg.ResolveEffectiveValueAsync("finops.release.budget_gate.approvers", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Cfg("finops.release.budget_gate.approvers", """["alice@corp.com","bob@corp.com"]"""));
        cfg.ResolveEffectiveValueAsync("finops.budget_alert_threshold", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Cfg("finops.budget_alert_threshold", "75"));

        var sut = new GetFinOpsConfigFeature.Handler(cfg, dt);
        var result = await sut.Handle(new GetFinOpsConfigFeature.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Currency.Should().Be("BRL");
        result.Value.BudgetGateEnabled.Should().BeTrue();
        result.Value.BlockOnExceed.Should().BeFalse();
        result.Value.RequireApproval.Should().BeTrue();
        result.Value.AlertThresholdPct.Should().Be(75m);
        result.Value.Approvers.Should().HaveCount(2).And.Contain("alice@corp.com");
    }

    // ── EvaluateReleaseBudgetGate ─────────────────────────────────────

    [Fact]
    public async Task EvaluateGate_Returns_Allow_When_Gate_Disabled()
    {
        var (cfg, dt) = CreateMocks();
        cfg.ResolveEffectiveValueAsync(Arg.Any<string>(), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);

        var sut = new EvaluateGateFeature.Handler(cfg, dt);
        var query = new EvaluateGateFeature.Query(SampleReleaseId, "payment-api", "Production", 50m, 40m, 7);
        var result = await sut.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(EvaluateGateFeature.BudgetGateAction.Allow);
        result.Value.Reason.Should().Contain("disabled");
    }

    [Fact]
    public async Task EvaluateGate_Returns_Allow_When_Under_Threshold()
    {
        var (cfg, dt) = CreateMocks();
        cfg.ResolveEffectiveValueAsync(Arg.Any<string>(), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);
        cfg.ResolveEffectiveValueAsync("finops.release.budget_gate.enabled", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Cfg("finops.release.budget_gate.enabled", "true"));
        cfg.ResolveEffectiveValueAsync("finops.budget_alert_threshold", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Cfg("finops.budget_alert_threshold", "80"));

        var sut = new EvaluateGateFeature.Handler(cfg, dt);
        // 50 vs 45 = +11.1% delta, below 80% threshold
        var query = new EvaluateGateFeature.Query(SampleReleaseId, "payment-api", "Production", 50m, 45m, 7);
        var result = await sut.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(EvaluateGateFeature.BudgetGateAction.Allow);
        result.Value.CostDeltaPct.Should().BeApproximately(11.11m, 0.1m);
    }

    [Fact]
    public async Task EvaluateGate_Returns_Warn_When_Over_But_Block_Disabled()
    {
        var (cfg, dt) = CreateMocks();
        cfg.ResolveEffectiveValueAsync(Arg.Any<string>(), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);
        cfg.ResolveEffectiveValueAsync("finops.release.budget_gate.enabled", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Cfg("finops.release.budget_gate.enabled", "true"));
        cfg.ResolveEffectiveValueAsync("finops.budget_alert_threshold", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Cfg("finops.budget_alert_threshold", "10"));
        cfg.ResolveEffectiveValueAsync("finops.release.budget_gate.block_on_exceed", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Cfg("finops.release.budget_gate.block_on_exceed", "false"));

        var sut = new EvaluateGateFeature.Handler(cfg, dt);
        // 100 vs 50 = +100% delta, above 10% threshold
        var query = new EvaluateGateFeature.Query(SampleReleaseId, "payment-api", "Production", 100m, 50m, 7);
        var result = await sut.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(EvaluateGateFeature.BudgetGateAction.Warn);
    }

    [Fact]
    public async Task EvaluateGate_Returns_RequireApproval_When_Block_And_ApprovalEnabled()
    {
        var (cfg, dt) = CreateMocks();
        cfg.ResolveEffectiveValueAsync(Arg.Any<string>(), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);
        cfg.ResolveEffectiveValueAsync("finops.release.budget_gate.enabled", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Cfg("finops.release.budget_gate.enabled", "true"));
        cfg.ResolveEffectiveValueAsync("finops.budget_alert_threshold", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Cfg("finops.budget_alert_threshold", "10"));
        cfg.ResolveEffectiveValueAsync("finops.release.budget_gate.block_on_exceed", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Cfg("finops.release.budget_gate.block_on_exceed", "true"));
        cfg.ResolveEffectiveValueAsync("finops.release.budget_gate.require_approval", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Cfg("finops.release.budget_gate.require_approval", "true"));

        var sut = new EvaluateGateFeature.Handler(cfg, dt);
        var query = new EvaluateGateFeature.Query(SampleReleaseId, "payment-api", "Production", 100m, 50m, 7);
        var result = await sut.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(EvaluateGateFeature.BudgetGateAction.RequireApproval);
    }

    [Fact]
    public async Task EvaluateGate_Returns_Block_When_Block_And_ApprovalDisabled()
    {
        var (cfg, dt) = CreateMocks();
        cfg.ResolveEffectiveValueAsync(Arg.Any<string>(), Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((EffectiveConfigurationDto?)null);
        cfg.ResolveEffectiveValueAsync("finops.release.budget_gate.enabled", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Cfg("finops.release.budget_gate.enabled", "true"));
        cfg.ResolveEffectiveValueAsync("finops.budget_alert_threshold", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Cfg("finops.budget_alert_threshold", "10"));
        cfg.ResolveEffectiveValueAsync("finops.release.budget_gate.block_on_exceed", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Cfg("finops.release.budget_gate.block_on_exceed", "true"));
        cfg.ResolveEffectiveValueAsync("finops.release.budget_gate.require_approval", Arg.Any<ConfigurationScope>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Cfg("finops.release.budget_gate.require_approval", "false"));

        var sut = new EvaluateGateFeature.Handler(cfg, dt);
        var query = new EvaluateGateFeature.Query(SampleReleaseId, "payment-api", "Production", 100m, 50m, 7);
        var result = await sut.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(EvaluateGateFeature.BudgetGateAction.Block);
    }

    // ── CreateFinOpsBudgetApproval ────────────────────────────────────

    [Fact]
    public async Task CreateApproval_Should_Create_Pending_Request()
    {
        var repo = Substitute.For<IFinOpsBudgetApprovalRepository>();
        var uow = Substitute.For<IGovernanceUnitOfWork>();
        var dt = Substitute.For<IDateTimeProvider>();
        dt.UtcNow.Returns(FixedNow);

        var sut = new CreateFinOpsBudgetApproval.Handler(repo, uow, dt);
        var command = new CreateFinOpsBudgetApproval.Command(
            ReleaseId: SampleReleaseId,
            ServiceName: "payment-api",
            Environment: "Production",
            ActualCost: 1500m,
            BaselineCost: 1000m,
            CostDeltaPct: 50m,
            Currency: "USD",
            RequestedBy: "dev@corp.com",
            Justification: "Black Friday spike — approved by product owner");

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReleaseId.Should().Be(SampleReleaseId);
        result.Value.Status.Should().Be("Pending");
        result.Value.ServiceName.Should().Be("payment-api");
        await repo.Received(1).AddAsync(Arg.Is<FinOpsBudgetApproval>(a =>
            a.RequestedBy == "dev@corp.com" &&
            a.Status == FinOpsBudgetApprovalStatus.Pending), Arg.Any<CancellationToken>());
    }

    // ── ResolveFinOpsBudgetApproval ───────────────────────────────────

    [Fact]
    public async Task ResolveApproval_Should_Approve_Pending_Request()
    {
        var approval = FinOpsBudgetApproval.Create(
            SampleReleaseId, "payment-api", "Production", 1500m, 1000m, 50m, "USD", "dev@corp.com", null, FixedNow);

        var repo = Substitute.For<IFinOpsBudgetApprovalRepository>();
        repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(approval);
        var uow = Substitute.For<IGovernanceUnitOfWork>();
        var dt = Substitute.For<IDateTimeProvider>();
        dt.UtcNow.Returns(FixedNow);

        var sut = new ResolveFinOpsBudgetApproval.Handler(repo, uow, dt);
        var command = new ResolveFinOpsBudgetApproval.Command(approval.Id.Value, true, "manager@corp.com", "Approved — critical release");
        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Approved");
        result.Value.ResolvedBy.Should().Be("manager@corp.com");
        repo.Received(1).Update(Arg.Is<FinOpsBudgetApproval>(a => a.Status == FinOpsBudgetApprovalStatus.Approved));
    }

    [Fact]
    public async Task ResolveApproval_Should_Reject_Pending_Request()
    {
        var approval = FinOpsBudgetApproval.Create(
            SampleReleaseId, "payment-api", "Production", 1500m, 1000m, 50m, "USD", "dev@corp.com", null, FixedNow);

        var repo = Substitute.For<IFinOpsBudgetApprovalRepository>();
        repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(approval);
        var uow = Substitute.For<IGovernanceUnitOfWork>();
        var dt = Substitute.For<IDateTimeProvider>();
        dt.UtcNow.Returns(FixedNow);

        var sut = new ResolveFinOpsBudgetApproval.Handler(repo, uow, dt);
        var command = new ResolveFinOpsBudgetApproval.Command(approval.Id.Value, false, "manager@corp.com", "Over budget without justification");
        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Rejected");
    }

    [Fact]
    public async Task ResolveApproval_Should_Fail_When_Already_Resolved()
    {
        var approval = FinOpsBudgetApproval.Create(
            SampleReleaseId, "payment-api", "Production", 1500m, 1000m, 50m, "USD", "dev@corp.com", null, FixedNow);
        approval.Approve("manager@corp.com", null, FixedNow);

        var repo = Substitute.For<IFinOpsBudgetApprovalRepository>();
        repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(approval);
        var uow = Substitute.For<IGovernanceUnitOfWork>();
        var dt = Substitute.For<IDateTimeProvider>();
        dt.UtcNow.Returns(FixedNow);

        var sut = new ResolveFinOpsBudgetApproval.Handler(repo, uow, dt);
        var command = new ResolveFinOpsBudgetApproval.Command(approval.Id.Value, true, "another@corp.com", null);
        var result = await sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // ── ListFinOpsBudgetApprovals ─────────────────────────────────────

    [Fact]
    public async Task ListApprovals_Should_Return_All_By_Default()
    {
        var approvals = new[]
        {
            FinOpsBudgetApproval.Create(SampleReleaseId, "svc-a", "Production", 100m, 80m, 25m, "USD", "dev1@corp.com", null, FixedNow),
            FinOpsBudgetApproval.Create(Guid.NewGuid(), "svc-b", "Production", 200m, 150m, 33m, "EUR", "dev2@corp.com", null, FixedNow),
        };

        var repo = Substitute.For<IFinOpsBudgetApprovalRepository>();
        repo.ListAsync(null, null, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<FinOpsBudgetApproval>)approvals);

        var sut = new ListFinOpsBudgetApprovals.Handler(repo);
        var result = await sut.Handle(new ListFinOpsBudgetApprovals.Query(null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items[0].ServiceName.Should().Be("svc-a");
        result.Value.Items[1].Currency.Should().Be("EUR");
    }
}
