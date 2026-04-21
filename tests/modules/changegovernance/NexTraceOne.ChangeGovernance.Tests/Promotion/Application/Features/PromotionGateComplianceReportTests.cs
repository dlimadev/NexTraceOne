using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Promotion.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Promotion.Features.GetPromotionGateComplianceReport;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;

namespace NexTraceOne.ChangeGovernance.Tests.Promotion.Application.Features;

/// <summary>
/// Testes unitários para Wave O.3 — GetPromotionGateComplianceReport.
/// Cobre totais de avaliação, taxa global de aprovação, distribuição por tipo de gate,
/// contagem de overrides, top gates com mais falhas e comportamento com dados vazios.
/// </summary>
public sealed class PromotionGateComplianceReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static DeploymentEnvironment CreateEnv()
        => DeploymentEnvironment.Create("Production", "Prod", 1, true, true, FixedNow);

    private static PromotionGate CreateGate(DeploymentEnvironment env, string gateName, string gateType)
        => PromotionGate.Create(env.Id, gateName, gateType, isRequired: true);

    private static GateEvaluation CreateEvaluation(
        PromotionGateId gateId,
        bool passed,
        string? overrideJustification = null)
    {
        var requestId = new PromotionRequestId(Guid.NewGuid());
        var eval = GateEvaluation.Create(
            requestId: requestId,
            gateId: gateId,
            passed: passed,
            evaluatedBy: "system",
            details: null,
            evaluatedAt: FixedNow);  // within lookback window

        if (overrideJustification is not null && !passed)
        {
            eval.Override(overrideJustification, "admin", FixedNow);
        }

        return eval;
    }

    private static (
        IDeploymentEnvironmentRepository envRepo,
        IPromotionGateRepository gateRepo,
        IGateEvaluationRepository evalRepo)
        MakeRepos(
            DeploymentEnvironment[] envs,
            (DeploymentEnvironment Env, PromotionGate Gate)[] gates,
            (PromotionGate Gate, GateEvaluation Eval)[] evaluations)
    {
        var envRepo = Substitute.For<IDeploymentEnvironmentRepository>();
        envRepo.ListActiveAsync(Arg.Any<CancellationToken>()).Returns(envs);

        var gateRepo = Substitute.For<IPromotionGateRepository>();
        foreach (var env in envs)
        {
            var envGates = gates.Where(g => g.Env.Id == env.Id).Select(g => g.Gate).ToList();
            gateRepo.ListByEnvironmentIdAsync(env.Id, Arg.Any<CancellationToken>()).Returns(envGates);
        }

        var evalRepo = Substitute.For<IGateEvaluationRepository>();
        foreach (var gate in gates.Select(g => g.Gate))
        {
            var gateEvals = evaluations.Where(e => e.Gate.Id == gate.Id).Select(e => e.Eval).ToList();
            evalRepo.ListByGateIdAsync(gate.Id, Arg.Any<CancellationToken>()).Returns(gateEvals);
        }

        return (envRepo, gateRepo, evalRepo);
    }

    // ── Empty report ──────────────────────────────────────────────────────

    [Fact]
    public async Task Report_IsSuccess_When_NoEnvironments()
    {
        var envRepo = Substitute.For<IDeploymentEnvironmentRepository>();
        envRepo.ListActiveAsync(Arg.Any<CancellationToken>()).Returns([]);

        var gateRepo = Substitute.For<IPromotionGateRepository>();
        var evalRepo = Substitute.For<IGateEvaluationRepository>();

        var handler = new GetPromotionGateComplianceReport.Handler(
            gateRepo, evalRepo, envRepo, CreateClock());

        var result = await handler.Handle(
            new GetPromotionGateComplianceReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalEvaluations.Should().Be(0);
        result.Value.GlobalPassRate.Should().Be(0m);
        result.Value.ByGateType.Should().BeEmpty();
        result.Value.TopFailingGates.Should().BeEmpty();
    }

    [Fact]
    public async Task Report_IsSuccess_When_NoEvaluations()
    {
        var env = CreateEnv();
        var gate = CreateGate(env, "AllTestsPassed", "QualityGate");
        var (envRepo, gateRepo, evalRepo) = MakeRepos(
            [env], [(env, gate)], []);

        var handler = new GetPromotionGateComplianceReport.Handler(
            gateRepo, evalRepo, envRepo, CreateClock());

        var result = await handler.Handle(
            new GetPromotionGateComplianceReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalEvaluations.Should().Be(0);
        result.Value.TotalPassed.Should().Be(0);
        result.Value.TotalFailed.Should().Be(0);
        result.Value.TotalOverridden.Should().Be(0);
    }

    // ── Pass/Fail totals ──────────────────────────────────────────────────

    [Fact]
    public async Task Totals_Correct_Mixed_Results()
    {
        var env = CreateEnv();
        var gate = CreateGate(env, "ScanPassed", "SecurityGate");
        var evals = new[]
        {
            CreateEvaluation(gate.Id, passed: true),
            CreateEvaluation(gate.Id, passed: true),
            CreateEvaluation(gate.Id, passed: false),
        };
        var (envRepo, gateRepo, evalRepo) = MakeRepos(
            [env], [(env, gate)], evals.Select(e => (gate, e)).ToArray());

        var handler = new GetPromotionGateComplianceReport.Handler(
            gateRepo, evalRepo, envRepo, CreateClock());

        var result = await handler.Handle(
            new GetPromotionGateComplianceReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalEvaluations.Should().Be(3);
        result.Value.TotalPassed.Should().Be(2);
        result.Value.TotalFailed.Should().Be(1);
        result.Value.TotalOverridden.Should().Be(0);
    }

    [Fact]
    public async Task GlobalPassRate_Correct()
    {
        var env = CreateEnv();
        var gate = CreateGate(env, "MinApprovals", "ApprovalGate");
        var evals = new[]
        {
            CreateEvaluation(gate.Id, passed: true),
            CreateEvaluation(gate.Id, passed: true),
            CreateEvaluation(gate.Id, passed: true),
            CreateEvaluation(gate.Id, passed: false),
        };
        var (envRepo, gateRepo, evalRepo) = MakeRepos(
            [env], [(env, gate)], evals.Select(e => (gate, e)).ToArray());

        var handler = new GetPromotionGateComplianceReport.Handler(
            gateRepo, evalRepo, envRepo, CreateClock());

        var result = await handler.Handle(
            new GetPromotionGateComplianceReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.GlobalPassRate.Should().Be(75m);
    }

    // ── Override tracking ─────────────────────────────────────────────────

    [Fact]
    public async Task Override_Counted_Separately_From_Passed()
    {
        var env = CreateEnv();
        var gate = CreateGate(env, "AllTestsPassed", "QualityGate");

        var eval1 = CreateEvaluation(gate.Id, passed: true);           // normal pass
        var eval2 = GateEvaluation.Create(
            new PromotionRequestId(Guid.NewGuid()), gate.Id,
            passed: false, evaluatedBy: "system", details: null,
            evaluatedAt: FixedNow);
        eval2.Override("emergency fix", "admin", FixedNow);           // override

        var (envRepo, gateRepo, evalRepo) = MakeRepos(
            [env], [(env, gate)], [(gate, eval1), (gate, eval2)]);

        var handler = new GetPromotionGateComplianceReport.Handler(
            gateRepo, evalRepo, envRepo, CreateClock());

        var result = await handler.Handle(
            new GetPromotionGateComplianceReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalPassed.Should().Be(1);
        result.Value.TotalOverridden.Should().Be(1);
        result.Value.TotalFailed.Should().Be(0);
        result.Value.GlobalPassRate.Should().Be(100m); // both contributed to pass rate
    }

    // ── By gate type ──────────────────────────────────────────────────────

    [Fact]
    public async Task ByGateType_Groups_Correctly()
    {
        var env = CreateEnv();
        var gateA = CreateGate(env, "ScanPassed", "SecurityGate");
        var gateB = CreateGate(env, "AllTests", "QualityGate");

        var evalsA = new[] { CreateEvaluation(gateA.Id, true), CreateEvaluation(gateA.Id, false) };
        var evalsB = new[] { CreateEvaluation(gateB.Id, true), CreateEvaluation(gateB.Id, true) };

        var allGates = new[] { (env, gateA), (env, gateB) };
        var allEvals = evalsA.Select(e => (gateA, e)).Concat(evalsB.Select(e => (gateB, e))).ToArray();

        var (envRepo, gateRepo, evalRepo) = MakeRepos([env], allGates, allEvals);

        var handler = new GetPromotionGateComplianceReport.Handler(
            gateRepo, evalRepo, envRepo, CreateClock());

        var result = await handler.Handle(
            new GetPromotionGateComplianceReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ByGateType.Count.Should().Be(2);

        var securityEntry = result.Value.ByGateType.FirstOrDefault(g => g.GateType == "SecurityGate");
        securityEntry.Should().NotBeNull();
        securityEntry!.TotalEvaluations.Should().Be(2);
        securityEntry.PassedCount.Should().Be(1);
        securityEntry.FailedCount.Should().Be(1);
        securityEntry.PassRate.Should().Be(50m);

        var qualityEntry = result.Value.ByGateType.FirstOrDefault(g => g.GateType == "QualityGate");
        qualityEntry.Should().NotBeNull();
        qualityEntry!.PassRate.Should().Be(100m);
    }

    // ── Top failing gates ─────────────────────────────────────────────────

    [Fact]
    public async Task TopFailingGates_OnlyGatesWithFailures()
    {
        var env = CreateEnv();
        var gateOk = CreateGate(env, "AlwaysPasses", "ApprovalGate");
        var gateFail = CreateGate(env, "AlwaysFails", "SecurityGate");

        var okEvals = new[] { CreateEvaluation(gateOk.Id, true) };
        var failEvals = new[] { CreateEvaluation(gateFail.Id, false), CreateEvaluation(gateFail.Id, false) };

        var (envRepo, gateRepo, evalRepo) = MakeRepos(
            [env],
            [(env, gateOk), (env, gateFail)],
            okEvals.Select(e => (gateOk, e)).Concat(failEvals.Select(e => (gateFail, e))).ToArray());

        var handler = new GetPromotionGateComplianceReport.Handler(
            gateRepo, evalRepo, envRepo, CreateClock());

        var result = await handler.Handle(
            new GetPromotionGateComplianceReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopFailingGates.Should().HaveCount(1);
        result.Value.TopFailingGates.First().GateType.Should().Be("SecurityGate");
        result.Value.TopFailingGates.First().FailedCount.Should().Be(2);
        result.Value.TopFailingGates.First().FailRate.Should().Be(100m);
    }

    [Fact]
    public async Task TopFailingGates_LimitedByCount()
    {
        var env = CreateEnv();
        var gates = Enumerable.Range(0, 10)
            .Select(i => CreateGate(env, $"gate-{i}", "SecurityGate"))
            .ToArray();

        var gateTuples = gates.Select(g => (env, g)).ToArray();
        var evalTuples = gates.SelectMany(g =>
            new[] { CreateEvaluation(g.Id, false) }.Select(e => (g, e))).ToArray();

        var (envRepo, gateRepo, evalRepo) = MakeRepos([env], gateTuples, evalTuples);

        var handler = new GetPromotionGateComplianceReport.Handler(
            gateRepo, evalRepo, envRepo, CreateClock());

        var result = await handler.Handle(
            new GetPromotionGateComplianceReport.Query(TenantId, TopFailingGatesCount: 3),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopFailingGates.Count.Should().Be(3);
    }

    // ── Report metadata ───────────────────────────────────────────────────

    [Fact]
    public async Task Report_Metadata_Correct()
    {
        var envRepo = Substitute.For<IDeploymentEnvironmentRepository>();
        envRepo.ListActiveAsync(Arg.Any<CancellationToken>()).Returns([]);

        var handler = new GetPromotionGateComplianceReport.Handler(
            Substitute.For<IPromotionGateRepository>(),
            Substitute.For<IGateEvaluationRepository>(),
            envRepo,
            CreateClock());

        var result = await handler.Handle(
            new GetPromotionGateComplianceReport.Query(TenantId, LookbackDays: 14),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(TenantId);
        result.Value.LookbackDays.Should().Be(14);
        result.Value.GeneratedAt.Should().Be(FixedNow);
        result.Value.From.Should().BeCloseTo(FixedNow.AddDays(-14), TimeSpan.FromSeconds(1));
    }

    // ── Validator ─────────────────────────────────────────────────────────

    [Fact]
    public void Validator_Rejects_EmptyTenantId()
    {
        var validator = new GetPromotionGateComplianceReport.Validator();
        var result = validator.Validate(new GetPromotionGateComplianceReport.Query(Guid.Empty));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Rejects_LookbackDays_Zero()
    {
        var validator = new GetPromotionGateComplianceReport.Validator();
        var result = validator.Validate(new GetPromotionGateComplianceReport.Query(TenantId, LookbackDays: 0));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Rejects_TopFailingGatesCount_Zero()
    {
        var validator = new GetPromotionGateComplianceReport.Validator();
        var result = validator.Validate(new GetPromotionGateComplianceReport.Query(TenantId, TopFailingGatesCount: 0));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Accepts_Valid_Query()
    {
        var validator = new GetPromotionGateComplianceReport.Validator();
        var result = validator.Validate(new GetPromotionGateComplianceReport.Query(TenantId, LookbackDays: 30, TopFailingGatesCount: 10));
        result.IsValid.Should().BeTrue();
    }
}
