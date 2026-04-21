using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetRiskTrendReport;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Entities;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;

namespace NexTraceOne.ChangeGovernance.Tests.Compliance;

/// <summary>
/// Testes unitários para Wave N.2 — GetRiskTrendReport.
/// Cobre distribuição de risco, ranking de serviços de alto risco,
/// score médio global, médias por dimensão e comportamento com dados vazios.
/// </summary>
public sealed class RiskTrendReportTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);
    private const string TenantId = "tenant-risk-trend";
    private static readonly Guid ServiceA = Guid.NewGuid();
    private static readonly Guid ServiceB = Guid.NewGuid();
    private static readonly Guid ServiceC = Guid.NewGuid();

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(Now);
        return clock;
    }

    private static ServiceRiskProfile MakeProfile(
        Guid serviceId,
        string name,
        int vulnerability,
        int changeFailure,
        int blastRadius,
        int policyViolation)
        => ServiceRiskProfile.Compute(
            tenantId: TenantId,
            serviceAssetId: serviceId,
            serviceName: name,
            vulnerabilityScore: vulnerability,
            changeFailureScore: changeFailure,
            blastRadiusScore: blastRadius,
            policyViolationScore: policyViolation,
            activeSignals: [],
            computedAt: Now);

    // ── Empty report ──────────────────────────────────────────────────────

    [Fact]
    public async Task Report_Empty_When_No_Profiles()
    {
        var repo = Substitute.For<IServiceRiskProfileRepository>();
        repo.ListByTenantRankedAsync(TenantId, 500, Arg.Any<CancellationToken>()).Returns([]);

        var handler = new GetRiskTrendReport.Handler(repo, CreateClock());
        var result = await handler.Handle(
            new GetRiskTrendReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalServicesAnalyzed.Should().Be(0);
        result.Value.HighCriticalPercent.Should().Be(0m);
        result.Value.TopHighRiskServices.Should().BeEmpty();
        result.Value.GeneratedAt.Should().Be(Now);
    }

    // ── Risk distribution ─────────────────────────────────────────────────

    [Fact]
    public async Task RiskDistribution_AllCritical_WhenScoresHigh()
    {
        // All vuln=100 → Critical
        var profiles = new[]
        {
            MakeProfile(ServiceA, "svc-a", 100, 90, 80, 70),
            MakeProfile(ServiceB, "svc-b", 100, 95, 85, 75)
        };
        var repo = Substitute.For<IServiceRiskProfileRepository>();
        repo.ListByTenantRankedAsync(TenantId, 500, Arg.Any<CancellationToken>()).Returns(profiles);

        var handler = new GetRiskTrendReport.Handler(repo, CreateClock());
        var result = await handler.Handle(
            new GetRiskTrendReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalServicesAnalyzed.Should().Be(2);
        result.Value.RiskDistribution.CriticalCount.Should().Be(2);
        result.Value.RiskDistribution.LowCount.Should().Be(0);
        result.Value.HighCriticalPercent.Should().Be(100m);
    }

    [Fact]
    public async Task RiskDistribution_Mixed_Levels()
    {
        var profiles = new[]
        {
            MakeProfile(ServiceA, "svc-low", 0, 0, 0, 0),        // Negligible
            MakeProfile(ServiceB, "svc-med", 50, 30, 20, 10),     // Medium
            MakeProfile(ServiceC, "svc-crit", 100, 90, 80, 70)    // Critical
        };
        var repo = Substitute.For<IServiceRiskProfileRepository>();
        repo.ListByTenantRankedAsync(TenantId, 500, Arg.Any<CancellationToken>()).Returns(profiles);

        var handler = new GetRiskTrendReport.Handler(repo, CreateClock());
        var result = await handler.Handle(
            new GetRiskTrendReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalServicesAnalyzed.Should().Be(3);
        result.Value.RiskDistribution.NegligibleCount.Should().Be(1);
        result.Value.RiskDistribution.CriticalCount.Should().Be(1);
        result.Value.HighCriticalPercent.Should().BeApproximately(33.3m, 0.2m);
    }

    // ── TopHighRiskServices ───────────────────────────────────────────────

    [Fact]
    public async Task TopHighRiskServices_FiltersBelow_Threshold()
    {
        var highRisk = MakeProfile(ServiceA, "svc-high", 80, 70, 60, 50);
        var lowRisk = MakeProfile(ServiceB, "svc-low", 0, 0, 0, 0);

        var profiles = new[] { highRisk, lowRisk };
        var repo = Substitute.For<IServiceRiskProfileRepository>();
        repo.ListByTenantRankedAsync(TenantId, 500, Arg.Any<CancellationToken>()).Returns(profiles);

        var handler = new GetRiskTrendReport.Handler(repo, CreateClock());
        var result = await handler.Handle(
            new GetRiskTrendReport.Query(TenantId, HighRiskThreshold: 60), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopHighRiskServices.Should().HaveCount(1);
        result.Value.TopHighRiskServices[0].ServiceName.Should().Be("svc-high");
    }

    [Fact]
    public async Task TopHighRiskServices_CappedAt_MaxHighRiskServices()
    {
        var profiles = Enumerable.Range(1, 10)
            .Select(i => MakeProfile(Guid.NewGuid(), $"svc-{i:D2}", 90, 80, 70, 60))
            .ToArray();
        var repo = Substitute.For<IServiceRiskProfileRepository>();
        repo.ListByTenantRankedAsync(TenantId, 500, Arg.Any<CancellationToken>()).Returns(profiles);

        var handler = new GetRiskTrendReport.Handler(repo, CreateClock());
        var result = await handler.Handle(
            new GetRiskTrendReport.Query(TenantId, MaxHighRiskServices: 5, HighRiskThreshold: 1),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopHighRiskServices.Should().HaveCount(5);
    }

    // ── DimensionAverages ─────────────────────────────────────────────────

    [Fact]
    public async Task DimensionAverages_AreCorrect()
    {
        // svc-a: vuln=80, change=60, blast=40, policy=20
        // svc-b: vuln=60, change=40, blast=20, policy=0
        // avg: vuln=70, change=50, blast=30, policy=10
        var profiles = new[]
        {
            MakeProfile(ServiceA, "svc-a", 80, 60, 40, 20),
            MakeProfile(ServiceB, "svc-b", 60, 40, 20, 0)
        };
        var repo = Substitute.For<IServiceRiskProfileRepository>();
        repo.ListByTenantRankedAsync(TenantId, 500, Arg.Any<CancellationToken>()).Returns(profiles);

        var handler = new GetRiskTrendReport.Handler(repo, CreateClock());
        var result = await handler.Handle(
            new GetRiskTrendReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DimensionAverages.AvgVulnerabilityScore.Should().Be(70m);
        result.Value.DimensionAverages.AvgChangeFailureScore.Should().Be(50m);
        result.Value.DimensionAverages.AvgBlastRadiusScore.Should().Be(30m);
        result.Value.DimensionAverages.AvgPolicyViolationScore.Should().Be(10m);
    }

    // ── AvgOverallRiskScore ───────────────────────────────────────────────

    [Fact]
    public async Task AvgOverallRiskScore_Matches_ProfileScores()
    {
        var p1 = MakeProfile(ServiceA, "svc-a", 100, 0, 0, 0);   // overall ≈ 40
        var p2 = MakeProfile(ServiceB, "svc-b", 0, 0, 0, 0);     // overall = 0
        var profiles = new[] { p1, p2 };
        var repo = Substitute.For<IServiceRiskProfileRepository>();
        repo.ListByTenantRankedAsync(TenantId, 500, Arg.Any<CancellationToken>()).Returns(profiles);

        var handler = new GetRiskTrendReport.Handler(repo, CreateClock());
        var result = await handler.Handle(
            new GetRiskTrendReport.Query(TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AvgOverallRiskScore.Should().Be(
            Math.Round((decimal)(p1.OverallScore + p2.OverallScore) / 2m, 1));
    }

    // ── Validator ─────────────────────────────────────────────────────────

    [Fact]
    public void Validator_Rejects_EmptyTenantId()
    {
        var validator = new GetRiskTrendReport.Validator();
        var result = validator.Validate(new GetRiskTrendReport.Query(TenantId: ""));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Validator_Rejects_InvalidMaxHighRiskServices()
    {
        var validator = new GetRiskTrendReport.Validator();
        var result = validator.Validate(new GetRiskTrendReport.Query(TenantId, MaxHighRiskServices: 0));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MaxHighRiskServices");
    }

    [Fact]
    public void Validator_Rejects_InvalidHighRiskThreshold()
    {
        var validator = new GetRiskTrendReport.Validator();
        var result = validator.Validate(new GetRiskTrendReport.Query(TenantId, HighRiskThreshold: 0));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "HighRiskThreshold");
    }

    [Fact]
    public void Validator_Accepts_Valid_Query()
    {
        var validator = new GetRiskTrendReport.Validator();
        var result = validator.Validate(
            new GetRiskTrendReport.Query(TenantId, MaxHighRiskServices: 20, HighRiskThreshold: 60));
        result.IsValid.Should().BeTrue();
    }
}
