using System.Linq;
using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Services;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>Testes do serviço de cálculo automático de ChangeIntelligenceScore (P5.3).</summary>
public sealed class ChangeScoreCalculatorTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly ChangeScoreCalculator _sut = new();

    [Theory]
    [InlineData(ChangeLevel.Operational, 0.0)]
    [InlineData(ChangeLevel.NonBreaking, 0.1)]
    [InlineData(ChangeLevel.Additive, 0.4)]
    [InlineData(ChangeLevel.Breaking, 1.0)]
    [InlineData(ChangeLevel.Publication, 0.1)]
    public void Compute_Should_DeriveBreakingChangeWeight_FromChangeLevel(ChangeLevel level, double expectedWeight)
    {
        var result = _sut.Compute(level, "staging", blastRadius: null);

        result.BreakingChangeWeight.Should().Be((decimal)expectedWeight);
        result.BreakingChangeReason.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("production", 1.0)]
    [InlineData("prod", 1.0)]
    [InlineData("staging", 0.6)]
    [InlineData("uat", 0.6)]
    [InlineData("development", 0.2)]
    [InlineData("dev", 0.2)]
    [InlineData("custom-env", 0.4)]
    public void Compute_Should_DeriveEnvironmentWeight_FromEnvironmentName(string env, double expectedWeight)
    {
        var result = _sut.Compute(ChangeLevel.NonBreaking, env, blastRadius: null);

        result.EnvironmentWeight.Should().Be((decimal)expectedWeight);
        result.EnvironmentReason.Should().NotBeEmpty();
    }

    [Fact]
    public void Compute_Should_UseZeroBlastRadiusWeight_WhenReportIsNull()
    {
        var result = _sut.Compute(ChangeLevel.Breaking, "production", blastRadius: null);

        result.BlastRadiusWeight.Should().Be(0.0m);
        result.ScoreSource.Should().Contain("blast_radius_pending");
    }

    [Fact]
    public void Compute_Should_UseHighBlastRadiusWeight_WhenManyConsumers()
    {
        var report = BlastRadiusReport.Calculate(
            ReleaseId.New(), Guid.NewGuid(),
            Enumerable.Range(1, 10).Select(i => $"Service{i}").ToList(),
            Enumerable.Range(11, 15).Select(i => $"Service{i}").ToList(),
            FixedNow);

        var result = _sut.Compute(ChangeLevel.Breaking, "production", report);

        result.BlastRadiusWeight.Should().Be(1.0m); // 25 consumers → high
        result.ScoreSource.Should().Contain("blast_radius");
        result.ScoreSource.Should().NotContain("pending");
    }

    [Fact]
    public void Compute_Should_UseModerateBlastRadiusWeight_WhenFewConsumers()
    {
        var report = BlastRadiusReport.Calculate(
            ReleaseId.New(), Guid.NewGuid(),
            new List<string> { "ServiceA", "ServiceB" },
            new List<string> { "ServiceC" },
            FixedNow);

        var result = _sut.Compute(ChangeLevel.NonBreaking, "staging", report);

        result.BlastRadiusWeight.Should().Be(0.3m); // 3 consumers → low
    }

    [Fact]
    public void Compute_Should_ProduceScoreInRange_ForAllCombinations()
    {
        foreach (var level in Enum.GetValues<ChangeLevel>())
        {
            foreach (var env in new[] { "production", "staging", "dev" })
            {
                var result = _sut.Compute(level, env, blastRadius: null);

                result.ComputedScore.Should().BeInRange(0m, 1m);
                result.BreakingChangeWeight.Should().BeInRange(0m, 1m);
                result.BlastRadiusWeight.Should().BeInRange(0m, 1m);
                result.EnvironmentWeight.Should().BeInRange(0m, 1m);
            }
        }
    }

    [Fact]
    public void Compute_Should_ProduceMaxScore_ForBreakingInProductionWithHighBlastRadius()
    {
        var report = BlastRadiusReport.Calculate(
            ReleaseId.New(), Guid.NewGuid(),
            Enumerable.Range(1, 25).Select(i => $"Service{i}").ToList(),
            [],
            FixedNow);

        var result = _sut.Compute(ChangeLevel.Breaking, "production", report);

        // (1.0 + 1.0 + 1.0) / 3 = 1.0
        result.ComputedScore.Should().Be(1.0m);
    }
}
