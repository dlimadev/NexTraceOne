using FluentAssertions;
using NexTraceOne.Contracts.Domain.Entities;
using NexTraceOne.Contracts.Domain.Enums;
using NexTraceOne.Contracts.Domain.Services;
using NexTraceOne.Contracts.Domain.ValueObjects;

namespace NexTraceOne.Contracts.Tests.Domain.Services;

/// <summary>
/// Testes unitários para <see cref="ContractScorecardCalculator"/>.
/// Valida o cálculo de scores técnicos em todas as dimensões.
/// </summary>
public sealed class ContractScorecardCalculatorTests
{
    [Fact]
    public void Compute_Should_ReturnHighScores_When_ModelIsComplete()
    {
        var model = new ContractCanonicalModel(
            ContractProtocol.OpenApi, "Complete API", "3.1.0", "A well-documented API",
            [new ContractOperation("listUsers", "listUsers", "List users", "GET", "/users", [], [], false, ["users"])],
            [new ContractSchemaElement("User", "object", false, "User entity")],
            ["bearerAuth"], ["https://api.example.com"], ["users"],
            1, 1, true, true, true);

        var scorecard = ContractScorecardCalculator.Compute(
            ContractVersionId.New(), model, ContractProtocol.OpenApi, 0, DateTimeOffset.UtcNow);

        scorecard.QualityScore.Should().BeGreaterThan(0.7m);
        scorecard.CompletenessScore.Should().BeGreaterThan(0.7m);
        scorecard.CompatibilityScore.Should().BeGreaterThan(0.7m);
        scorecard.OverallScore.Should().BeGreaterThan(0.5m);
        scorecard.RiskScore.Should().BeLessThan(0.3m);
    }

    [Fact]
    public void Compute_Should_ReturnLowScores_When_ModelIsIncomplete()
    {
        var model = new ContractCanonicalModel(
            ContractProtocol.OpenApi, "Minimal API", "", null,
            [], [], [], [], [],
            0, 0, false, false, false);

        var scorecard = ContractScorecardCalculator.Compute(
            ContractVersionId.New(), model, ContractProtocol.OpenApi, 10, DateTimeOffset.UtcNow);

        scorecard.QualityScore.Should().BeLessThan(0.6m);
        scorecard.CompletenessScore.Should().BeLessThan(0.6m);
        scorecard.RiskScore.Should().BeGreaterThan(0.3m);
    }

    [Fact]
    public void Compute_Should_IncrementRisk_When_SecurityMissing()
    {
        var withSecurity = new ContractCanonicalModel(
            ContractProtocol.OpenApi, "API", "1.0", null,
            [new ContractOperation("op1", "op1", "desc", "GET", "/", [], [])],
            [], ["bearer"], [], [],
            1, 0, true, false, true);

        var withoutSecurity = new ContractCanonicalModel(
            ContractProtocol.OpenApi, "API", "1.0", null,
            [new ContractOperation("op1", "op1", "desc", "GET", "/", [], [])],
            [], [], [], [],
            1, 0, false, false, true);

        var secure = ContractScorecardCalculator.Compute(ContractVersionId.New(), withSecurity, ContractProtocol.OpenApi, 0, DateTimeOffset.UtcNow);
        var insecure = ContractScorecardCalculator.Compute(ContractVersionId.New(), withoutSecurity, ContractProtocol.OpenApi, 0, DateTimeOffset.UtcNow);

        insecure.RiskScore.Should().BeGreaterThan(secure.RiskScore);
    }

    [Fact]
    public void Compute_Should_ClampScoresBetween0And1()
    {
        var model = new ContractCanonicalModel(
            ContractProtocol.OpenApi, "API", "1.0", null,
            [], [], [], [], [],
            0, 0, false, false, false);

        var scorecard = ContractScorecardCalculator.Compute(
            ContractVersionId.New(), model, ContractProtocol.OpenApi, 100, DateTimeOffset.UtcNow);

        scorecard.QualityScore.Should().BeInRange(0m, 1m);
        scorecard.CompletenessScore.Should().BeInRange(0m, 1m);
        scorecard.CompatibilityScore.Should().BeInRange(0m, 1m);
        scorecard.RiskScore.Should().BeInRange(0m, 1m);
        scorecard.OverallScore.Should().BeInRange(0m, 1m);
    }

    [Fact]
    public void Compute_Should_IncludeJustifications()
    {
        var model = new ContractCanonicalModel(
            ContractProtocol.OpenApi, "API", "1.0", null,
            [new ContractOperation("op", "op", null, "GET", "/", [], [])],
            [], [], [], [],
            1, 0, false, false, false);

        var scorecard = ContractScorecardCalculator.Compute(
            ContractVersionId.New(), model, ContractProtocol.OpenApi, 0, DateTimeOffset.UtcNow);

        scorecard.QualityJustification.Should().NotBeNullOrWhiteSpace();
        scorecard.CompletenessJustification.Should().NotBeNullOrWhiteSpace();
        scorecard.CompatibilityJustification.Should().NotBeNullOrWhiteSpace();
        scorecard.RiskJustification.Should().NotBeNullOrWhiteSpace();
    }
}
