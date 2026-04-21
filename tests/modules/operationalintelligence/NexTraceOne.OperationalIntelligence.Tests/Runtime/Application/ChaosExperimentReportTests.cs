using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetChaosExperimentReport;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Runtime.Application;

/// <summary>
/// Testes unitários para Wave K.1 — GetChaosExperimentReport.
/// Cobre relatório analítico de experimentos de chaos engineering.
/// </summary>
public sealed class ChaosExperimentReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly string TenantId = "00000000-0000-0000-0000-000000000001";

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static ChaosExperiment MakeExperiment(
        string serviceName = "payment-service",
        string experimentType = "latency-injection",
        string riskLevel = "Low",
        ExperimentStatus status = ExperimentStatus.Completed,
        DateTimeOffset? createdAt = null)
    {
        var at = createdAt ?? FixedNow.AddHours(-1);
        var experiment = ChaosExperiment.Create(
            tenantId: TenantId,
            serviceName: serviceName,
            environment: "Staging",
            experimentType: experimentType,
            description: null,
            riskLevel: riskLevel,
            durationSeconds: 60,
            targetPercentage: 10m,
            steps: new[] { "Step 1" },
            safetyChecks: new[] { "Check 1" },
            createdAt: at,
            createdBy: "user-1");

        if (status == ExperimentStatus.Running || status == ExperimentStatus.Completed || status == ExperimentStatus.Failed)
            experiment.Start(at.AddMinutes(1));

        if (status == ExperimentStatus.Completed)
            experiment.Complete(at.AddMinutes(2));
        else if (status == ExperimentStatus.Failed)
            experiment.Fail(at.AddMinutes(2));
        else if (status == ExperimentStatus.Cancelled)
            experiment.Cancel(at.AddMinutes(1));

        return experiment;
    }

    // ── Core analytics tests ─────────────────────────────────────────────────

    [Fact]
    public async Task GetChaosExperimentReport_Returns_EmptyReport_When_No_Experiments()
    {
        var repo = Substitute.For<IChaosExperimentRepository>();
        repo.ListAsync(TenantId, null, null, null, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ChaosExperiment>)[]);

        var handler = new GetChaosExperimentReport.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetChaosExperimentReport.Query(TenantId, 30), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalExperiments.Should().Be(0);
        result.Value.SuccessRatePercent.Should().Be(0m);
        result.Value.ByType.Should().BeEmpty();
        result.Value.TopServices.Should().BeEmpty();
        result.Value.MostRecentExperimentAt.Should().BeNull();
    }

    [Fact]
    public async Task GetChaosExperimentReport_Calculates_SuccessRate_Correctly()
    {
        var completed = MakeExperiment(status: ExperimentStatus.Completed);
        var failed = MakeExperiment(status: ExperimentStatus.Failed, experimentType: "pod-kill", riskLevel: "High");
        var repo = Substitute.For<IChaosExperimentRepository>();
        repo.ListAsync(TenantId, null, null, null, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ChaosExperiment>)[completed, failed]);

        var handler = new GetChaosExperimentReport.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetChaosExperimentReport.Query(TenantId, 30), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalExperiments.Should().Be(2);
        result.Value.SuccessRatePercent.Should().Be(50m);
    }

    [Fact]
    public async Task GetChaosExperimentReport_SuccessRate_Is_Zero_When_No_Completed_Or_Failed()
    {
        var planned = MakeExperiment(status: ExperimentStatus.Planned);
        var repo = Substitute.For<IChaosExperimentRepository>();
        repo.ListAsync(TenantId, null, null, null, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ChaosExperiment>)[planned]);

        var handler = new GetChaosExperimentReport.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetChaosExperimentReport.Query(TenantId, 30), CancellationToken.None);

        result.Value.SuccessRatePercent.Should().Be(0m);
    }

    [Fact]
    public async Task GetChaosExperimentReport_Groups_By_ExperimentType()
    {
        var latency1 = MakeExperiment(experimentType: "latency-injection");
        var latency2 = MakeExperiment(experimentType: "latency-injection");
        var podKill = MakeExperiment(experimentType: "pod-kill", riskLevel: "High");
        var repo = Substitute.For<IChaosExperimentRepository>();
        repo.ListAsync(TenantId, null, null, null, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ChaosExperiment>)[latency1, latency2, podKill]);

        var handler = new GetChaosExperimentReport.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetChaosExperimentReport.Query(TenantId, 30), CancellationToken.None);

        result.Value.ByType.Should().HaveCount(2);
        result.Value.ByType.First(t => t.ExperimentType == "latency-injection").Count.Should().Be(2);
        result.Value.ByType.First(t => t.ExperimentType == "pod-kill").Count.Should().Be(1);
    }

    [Fact]
    public async Task GetChaosExperimentReport_TopServices_Returns_Top5()
    {
        var experiments = Enumerable.Range(0, 7)
            .Select(i => MakeExperiment(serviceName: $"service-{i % 6}"))
            .ToList();
        var repo = Substitute.For<IChaosExperimentRepository>();
        repo.ListAsync(TenantId, null, null, null, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ChaosExperiment>)experiments);

        var handler = new GetChaosExperimentReport.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetChaosExperimentReport.Query(TenantId, 30), CancellationToken.None);

        result.Value.TopServices.Should().HaveCountLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task GetChaosExperimentReport_Filters_By_Period()
    {
        var recent = MakeExperiment(createdAt: FixedNow.AddDays(-5));
        var old = MakeExperiment(createdAt: FixedNow.AddDays(-40));
        var repo = Substitute.For<IChaosExperimentRepository>();
        repo.ListAsync(TenantId, null, null, null, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ChaosExperiment>)[recent, old]);

        var handler = new GetChaosExperimentReport.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetChaosExperimentReport.Query(TenantId, Days: 30), CancellationToken.None);

        result.Value.TotalExperiments.Should().Be(1);
    }

    [Fact]
    public async Task GetChaosExperimentReport_StatusDistribution_IsCorrect()
    {
        var completed = MakeExperiment(status: ExperimentStatus.Completed);
        var failed = MakeExperiment(status: ExperimentStatus.Failed, experimentType: "pod-kill", riskLevel: "High");
        var cancelled = MakeExperiment(status: ExperimentStatus.Cancelled, experimentType: "error-injection");
        var repo = Substitute.For<IChaosExperimentRepository>();
        repo.ListAsync(TenantId, null, null, null, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ChaosExperiment>)[completed, failed, cancelled]);

        var handler = new GetChaosExperimentReport.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetChaosExperimentReport.Query(TenantId, 30), CancellationToken.None);

        result.Value.ByStatus.Completed.Should().Be(1);
        result.Value.ByStatus.Failed.Should().Be(1);
        result.Value.ByStatus.Cancelled.Should().Be(1);
        result.Value.ByStatus.Planned.Should().Be(0);
        result.Value.ByStatus.Running.Should().Be(0);
    }

    [Fact]
    public async Task GetChaosExperimentReport_MostRecent_IsCorrect()
    {
        var older = MakeExperiment(createdAt: FixedNow.AddHours(-5));
        var newer = MakeExperiment(createdAt: FixedNow.AddHours(-1));
        var repo = Substitute.For<IChaosExperimentRepository>();
        repo.ListAsync(TenantId, null, null, null, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ChaosExperiment>)[older, newer]);

        var handler = new GetChaosExperimentReport.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetChaosExperimentReport.Query(TenantId, 30), CancellationToken.None);

        result.Value.MostRecentExperimentAt.Should().Be(FixedNow.AddHours(-1));
    }

    [Fact]
    public async Task GetChaosExperimentReport_ByRiskLevel_Groups_Correctly()
    {
        var low1 = MakeExperiment(riskLevel: "Low");
        var low2 = MakeExperiment(riskLevel: "Low");
        var high = MakeExperiment(riskLevel: "High", experimentType: "pod-kill");
        var repo = Substitute.For<IChaosExperimentRepository>();
        repo.ListAsync(TenantId, null, null, null, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ChaosExperiment>)[low1, low2, high]);

        var handler = new GetChaosExperimentReport.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetChaosExperimentReport.Query(TenantId, 30), CancellationToken.None);

        result.Value.ByRiskLevel.Should().HaveCount(2);
        result.Value.ByRiskLevel.First(r => r.RiskLevel == "Low").Count.Should().Be(2);
        result.Value.ByRiskLevel.First(r => r.RiskLevel == "High").Count.Should().Be(1);
    }

    [Fact]
    public async Task GetChaosExperimentReport_AverageDuration_IsCorrect()
    {
        var exp1 = MakeExperiment();  // durationSeconds = 60
        var exp2 = MakeExperiment();  // durationSeconds = 60
        var repo = Substitute.For<IChaosExperimentRepository>();
        repo.ListAsync(TenantId, null, null, null, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ChaosExperiment>)[exp1, exp2]);

        var handler = new GetChaosExperimentReport.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetChaosExperimentReport.Query(TenantId, 30), CancellationToken.None);

        result.Value.AverageDurationSeconds.Should().Be(60m);
    }

    [Fact]
    public void GetChaosExperimentReport_Validator_Rejects_Invalid_Days()
    {
        var validator = new GetChaosExperimentReport.Validator();
        var tooLow = validator.Validate(new GetChaosExperimentReport.Query(TenantId, 0));
        var tooHigh = validator.Validate(new GetChaosExperimentReport.Query(TenantId, 91));
        tooLow.IsValid.Should().BeFalse();
        tooHigh.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetChaosExperimentReport_Validator_Accepts_Valid_Query()
    {
        var validator = new GetChaosExperimentReport.Validator();
        var result = validator.Validate(new GetChaosExperimentReport.Query(TenantId, 30));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GetChaosExperimentReport_Returns_TenantId_And_Period_In_Response()
    {
        var repo = Substitute.For<IChaosExperimentRepository>();
        repo.ListAsync(TenantId, null, null, null, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<ChaosExperiment>)[]);

        var handler = new GetChaosExperimentReport.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetChaosExperimentReport.Query(TenantId, 45), CancellationToken.None);

        result.Value.TenantId.Should().Be(TenantId);
        result.Value.PeriodDays.Should().Be(45);
        result.Value.GeneratedAt.Should().Be(FixedNow);
    }
}
