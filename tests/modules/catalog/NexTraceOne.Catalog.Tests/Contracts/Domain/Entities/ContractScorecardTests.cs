using NexTraceOne.Contracts.Domain.Entities;
using NexTraceOne.Contracts.Domain.Enums;

namespace NexTraceOne.Contracts.Tests.Domain.Entities;

/// <summary>
/// Testes unitários para <see cref="ContractScorecard"/>.
/// Valida criação, clamping de scores e cálculo do score consolidado.
/// </summary>
public sealed class ContractScorecardTests
{
    [Fact]
    public void Create_Should_ClampScores_When_ValuesExceedRange()
    {
        var scorecard = ContractScorecard.Create(
            ContractVersionId.New(), ContractProtocol.OpenApi,
            1.5m, -0.5m, 0.8m, 2.0m,
            5, 3, true, true, true,
            "quality", "completeness", "compatibility", "risk",
            DateTimeOffset.UtcNow);

        scorecard.QualityScore.Should().Be(1.0m);
        scorecard.CompletenessScore.Should().Be(0.0m);
        scorecard.CompatibilityScore.Should().Be(0.8m);
        scorecard.RiskScore.Should().Be(1.0m);
        scorecard.OverallScore.Should().BeInRange(0m, 1m);
    }

    [Fact]
    public void Create_Should_CalculateWeightedOverallScore()
    {
        var scorecard = ContractScorecard.Create(
            ContractVersionId.New(), ContractProtocol.OpenApi,
            0.8m, 0.6m, 0.9m, 0.2m,
            3, 2, true, false, true,
            "good quality", "moderate completeness", "excellent compatibility", "low risk",
            DateTimeOffset.UtcNow);

        // Overall = (0.8*0.30) + (0.6*0.25) + (0.9*0.25) + ((1-0.2)*0.20) = 0.24 + 0.15 + 0.225 + 0.16 = 0.775
        scorecard.OverallScore.Should().BeApproximately(0.775m, 0.01m);
    }

    [Fact]
    public void Create_Should_StoreAllJustifications()
    {
        var scorecard = ContractScorecard.Create(
            ContractVersionId.New(), ContractProtocol.AsyncApi,
            0.5m, 0.5m, 0.5m, 0.5m,
            1, 0, false, false, false,
            "quality justification", "completeness justification",
            "compatibility justification", "risk justification",
            DateTimeOffset.UtcNow);

        scorecard.QualityJustification.Should().Be("quality justification");
        scorecard.CompletenessJustification.Should().Be("completeness justification");
        scorecard.CompatibilityJustification.Should().Be("compatibility justification");
        scorecard.RiskJustification.Should().Be("risk justification");
        scorecard.Protocol.Should().Be(ContractProtocol.AsyncApi);
    }

    [Fact]
    public void Create_Should_GenerateUniqueId()
    {
        var scorecard1 = ContractScorecard.Create(
            ContractVersionId.New(), ContractProtocol.OpenApi,
            0.5m, 0.5m, 0.5m, 0.5m, 1, 1, true, true, true,
            "q", "c", "co", "r", DateTimeOffset.UtcNow);

        var scorecard2 = ContractScorecard.Create(
            ContractVersionId.New(), ContractProtocol.OpenApi,
            0.5m, 0.5m, 0.5m, 0.5m, 1, 1, true, true, true,
            "q", "c", "co", "r", DateTimeOffset.UtcNow);

        scorecard1.Id.Should().NotBe(scorecard2.Id);
    }
}
