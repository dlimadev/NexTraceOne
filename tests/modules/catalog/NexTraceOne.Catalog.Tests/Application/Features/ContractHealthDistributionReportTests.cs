using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.GetContractHealthDistributionReport;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Tests.Application.Features;

/// <summary>
/// Testes unitários para Wave M.1 — GetContractHealthDistributionReport.
/// Cobre distribuição de scores de saúde de contratos por banda, médias de dimensão
/// e lista dos contratos mais críticos.
/// </summary>
public sealed class ContractHealthDistributionReportTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 21, 12, 0, 0, TimeSpan.Zero);

    private static IDateTimeProvider CreateClock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);
        return clock;
    }

    private static ContractHealthScore MakeScore(int overall)
    {
        // Target overall ≈ 'overall': adjust breakingChange and consumerImpact to drive it
        // ComputeOverall = bf*0.20 + ci*0.20 + rr*0.15 + ec*0.15 + pc*0.15 + d*0.15
        // If all six equal x, overall ≈ x (0.20+0.20+0.15+0.15+0.15+0.15=1.0)
        return ContractHealthScore.Create(
            apiAssetId: Guid.NewGuid(),
            breakingChangeFrequencyScore: overall,
            consumerImpactScore: overall,
            reviewRecencyScore: overall,
            exampleCoverageScore: overall,
            policyComplianceScore: overall,
            documentationScore: overall,
            degradationThreshold: 60,
            calculatedAt: FixedNow);
    }

    // ── Band classification ────────────────────────────────────────────────

    [Fact]
    public async Task Report_BandsCount_Correct_When_Mixed_Scores()
    {
        var scores = new[]
        {
            MakeScore(90), // Healthy (≥80)
            MakeScore(85), // Healthy
            MakeScore(70), // Fair (≥60 <80)
            MakeScore(65), // Fair
            MakeScore(50), // AtRisk (≥40 <60)
            MakeScore(30), // Critical (<40)
        };
        var repo = Substitute.For<IContractHealthScoreRepository>();
        repo.ListBelowThresholdAsync(101, Arg.Any<CancellationToken>()).Returns(scores);

        var handler = new GetContractHealthDistributionReport.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetContractHealthDistributionReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalContracts.Should().Be(6);
        result.Value.HealthyCount.Should().Be(2);
        result.Value.FairCount.Should().Be(2);
        result.Value.AtRiskCount.Should().Be(1);
        result.Value.CriticalCount.Should().Be(1);
    }

    [Fact]
    public async Task Report_HealthyPercent_Correct()
    {
        var scores = new[]
        {
            MakeScore(90),
            MakeScore(85),
            MakeScore(50),
            MakeScore(30),
        };
        var repo = Substitute.For<IContractHealthScoreRepository>();
        repo.ListBelowThresholdAsync(101, Arg.Any<CancellationToken>()).Returns(scores);

        var handler = new GetContractHealthDistributionReport.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetContractHealthDistributionReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HealthyPercent.Should().Be(50.0m); // 2/4
        result.Value.CriticalPercent.Should().Be(25.0m); // 1/4
    }

    [Fact]
    public async Task Report_Empty_When_No_Scores()
    {
        var repo = Substitute.For<IContractHealthScoreRepository>();
        repo.ListBelowThresholdAsync(101, Arg.Any<CancellationToken>()).Returns([]);

        var handler = new GetContractHealthDistributionReport.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetContractHealthDistributionReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalContracts.Should().Be(0);
        result.Value.HealthyCount.Should().Be(0);
        result.Value.CriticalCount.Should().Be(0);
        result.Value.DimensionAverages.AvgOverall.Should().Be(0m);
    }

    // ── TopCriticalContracts ───────────────────────────────────────────────

    [Fact]
    public async Task TopCriticalContracts_Sorted_By_Score_Ascending()
    {
        var scores = new[]
        {
            MakeScore(90),
            MakeScore(10),
            MakeScore(30),
            MakeScore(20),
        };
        var repo = Substitute.For<IContractHealthScoreRepository>();
        repo.ListBelowThresholdAsync(101, Arg.Any<CancellationToken>()).Returns(scores);

        var handler = new GetContractHealthDistributionReport.Handler(repo, CreateClock());
        var result = await handler.Handle(
            new GetContractHealthDistributionReport.Query(TopCriticalCount: 3), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopCriticalContracts.Should().HaveCount(3);
        result.Value.TopCriticalContracts[0].OverallScore.Should().Be(10);
        result.Value.TopCriticalContracts[1].OverallScore.Should().Be(20);
        result.Value.TopCriticalContracts[2].OverallScore.Should().Be(30);
    }

    [Fact]
    public async Task TopCriticalContracts_Limited_By_TopCriticalCount()
    {
        var scores = Enumerable.Range(1, 20).Select(i => MakeScore(i * 3)).ToArray();
        var repo = Substitute.For<IContractHealthScoreRepository>();
        repo.ListBelowThresholdAsync(101, Arg.Any<CancellationToken>()).Returns(scores);

        var handler = new GetContractHealthDistributionReport.Handler(repo, CreateClock());
        var result = await handler.Handle(
            new GetContractHealthDistributionReport.Query(TopCriticalCount: 5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TopCriticalContracts.Should().HaveCount(5);
    }

    // ── Band classification enum ───────────────────────────────────────────

    [Theory]
    [InlineData(90, "Healthy")]
    [InlineData(80, "Healthy")]
    [InlineData(79, "Fair")]
    [InlineData(60, "Fair")]
    [InlineData(59, "AtRisk")]
    [InlineData(40, "AtRisk")]
    [InlineData(39, "Critical")]
    [InlineData(0, "Critical")]
    public async Task Band_Classification_Correct(int score, string expectedBand)
    {
        var scores = new[] { MakeScore(score) };
        var repo = Substitute.For<IContractHealthScoreRepository>();
        repo.ListBelowThresholdAsync(101, Arg.Any<CancellationToken>()).Returns(scores);

        var handler = new GetContractHealthDistributionReport.Handler(repo, CreateClock());
        var result = await handler.Handle(
            new GetContractHealthDistributionReport.Query(TopCriticalCount: 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var entry = result.Value.TopCriticalContracts.First();
        entry.Band.ToString().Should().Be(expectedBand);
    }

    // ── Dimension averages ─────────────────────────────────────────────────

    [Fact]
    public async Task DimensionAverages_Correct_For_Two_Scores()
    {
        // Score 1: all dims = 80  → overall = 80
        // Score 2: all dims = 60  → overall = 60
        var s1 = ContractHealthScore.Create(Guid.NewGuid(), 80, 80, 80, 80, 80, 80, 50, FixedNow);
        var s2 = ContractHealthScore.Create(Guid.NewGuid(), 60, 60, 60, 60, 60, 60, 50, FixedNow);

        var repo = Substitute.For<IContractHealthScoreRepository>();
        repo.ListBelowThresholdAsync(101, Arg.Any<CancellationToken>()).Returns([s1, s2]);

        var handler = new GetContractHealthDistributionReport.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetContractHealthDistributionReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DimensionAverages.AvgBreakingChangeFrequency.Should().Be(70.0m);
        result.Value.DimensionAverages.AvgConsumerImpact.Should().Be(70.0m);
        result.Value.DimensionAverages.AvgDocumentation.Should().Be(70.0m);
        result.Value.DimensionAverages.AvgOverall.Should().Be(70.0m);
    }

    // ── Validation ────────────────────────────────────────────────────────

    [Fact]
    public void Validator_Rejects_Invalid_Thresholds()
    {
        var validator = new GetContractHealthDistributionReport.Validator();
        // FairThreshold > HealthyThreshold — invalid
        var result = validator.Validate(
            new GetContractHealthDistributionReport.Query(HealthyThreshold: 60, FairThreshold: 70, AtRiskThreshold: 40));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Rejects_TopCritical_Zero()
    {
        var validator = new GetContractHealthDistributionReport.Validator();
        var result = validator.Validate(
            new GetContractHealthDistributionReport.Query(TopCriticalCount: 0));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Accepts_Valid_Query()
    {
        var validator = new GetContractHealthDistributionReport.Validator();
        var result = validator.Validate(new GetContractHealthDistributionReport.Query());
        result.IsValid.Should().BeTrue();
    }

    // ── GeneratedAt ───────────────────────────────────────────────────────

    [Fact]
    public async Task Report_GeneratedAt_Uses_Clock()
    {
        var repo = Substitute.For<IContractHealthScoreRepository>();
        repo.ListBelowThresholdAsync(101, Arg.Any<CancellationToken>()).Returns([]);

        var handler = new GetContractHealthDistributionReport.Handler(repo, CreateClock());
        var result = await handler.Handle(new GetContractHealthDistributionReport.Query(), CancellationToken.None);

        result.Value.GeneratedAt.Should().Be(FixedNow);
    }

    // ── Thresholds surfaced in report ─────────────────────────────────────

    [Fact]
    public async Task Report_Returns_Configured_Thresholds()
    {
        var repo = Substitute.For<IContractHealthScoreRepository>();
        repo.ListBelowThresholdAsync(101, Arg.Any<CancellationToken>()).Returns([]);

        var query = new GetContractHealthDistributionReport.Query(
            HealthyThreshold: 75, FairThreshold: 55, AtRiskThreshold: 35);
        var handler = new GetContractHealthDistributionReport.Handler(repo, CreateClock());
        var result = await handler.Handle(query, CancellationToken.None);

        result.Value.HealthyThreshold.Should().Be(75);
        result.Value.FairThreshold.Should().Be(55);
        result.Value.AtRiskThreshold.Should().Be(35);
    }
}
