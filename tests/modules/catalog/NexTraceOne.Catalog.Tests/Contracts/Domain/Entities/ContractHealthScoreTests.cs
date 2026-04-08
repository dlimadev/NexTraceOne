using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Entities;

/// <summary>
/// Testes unitários para <see cref="ContractHealthScore"/>.
/// Valida criação, cálculo da média ponderada, estado de degradação e limites de validação.
/// </summary>
public sealed class ContractHealthScoreTests
{
    private static readonly Guid ValidApiAssetId = Guid.NewGuid();
    private static readonly DateTimeOffset FixedDate = new(2025, 06, 15, 10, 0, 0, TimeSpan.Zero);

    // ── Create com valores válidos ───────────────────────────────────

    [Fact]
    public void Create_Should_SetAllProperties_When_ValidValues()
    {
        var score = ContractHealthScore.Create(
            ValidApiAssetId, 80, 70, 60, 50, 90, 85, 50, FixedDate);

        score.Id.Value.Should().NotBeEmpty();
        score.ApiAssetId.Should().Be(ValidApiAssetId);
        score.BreakingChangeFrequencyScore.Should().Be(80);
        score.ConsumerImpactScore.Should().Be(70);
        score.ReviewRecencyScore.Should().Be(60);
        score.ExampleCoverageScore.Should().Be(50);
        score.PolicyComplianceScore.Should().Be(90);
        score.DocumentationScore.Should().Be(85);
        score.DegradationThreshold.Should().Be(50);
        score.CalculatedAt.Should().Be(FixedDate);
    }

    // ── Cálculo do OverallScore (média ponderada) ────────────────────

    [Fact]
    public void Create_Should_CalculateWeightedOverallScore()
    {
        // breaking=80(×0.20=16) + consumer=70(×0.20=14) + review=60(×0.15=9) +
        // examples=50(×0.15=7.5) + policy=90(×0.15=13.5) + docs=85(×0.15=12.75)
        // Total = 72.75 → Rounded = 73
        var score = ContractHealthScore.Create(
            ValidApiAssetId, 80, 70, 60, 50, 90, 85, 50, FixedDate);

        score.OverallScore.Should().Be(73);
    }

    [Fact]
    public void Create_Should_CalculateOverallScore_AllMaxValues()
    {
        var score = ContractHealthScore.Create(
            ValidApiAssetId, 100, 100, 100, 100, 100, 100, 50, FixedDate);

        score.OverallScore.Should().Be(100);
    }

    [Fact]
    public void Create_Should_CalculateOverallScore_AllZeroValues()
    {
        var score = ContractHealthScore.Create(
            ValidApiAssetId, 0, 0, 0, 0, 0, 0, 50, FixedDate);

        score.OverallScore.Should().Be(0);
    }

    // ── IsDegraded ──────────────────────────────────────────────────

    [Fact]
    public void Create_Should_SetIsDegraded_When_ScoreBelowThreshold()
    {
        // All low scores → overall ~20 which is below threshold 50
        var score = ContractHealthScore.Create(
            ValidApiAssetId, 20, 20, 20, 20, 20, 20, 50, FixedDate);

        score.IsDegraded.Should().BeTrue();
        score.OverallScore.Should().BeLessThan(50);
    }

    [Fact]
    public void Create_Should_NotSetIsDegraded_When_ScoreAboveThreshold()
    {
        var score = ContractHealthScore.Create(
            ValidApiAssetId, 80, 80, 80, 80, 80, 80, 50, FixedDate);

        score.IsDegraded.Should().BeFalse();
        score.OverallScore.Should().BeGreaterThanOrEqualTo(50);
    }

    [Fact]
    public void Create_Should_NotSetIsDegraded_When_ScoreEqualsThreshold()
    {
        // Exact threshold match: 50×0.20 + 50×0.20 + 50×0.15 + 50×0.15 + 50×0.15 + 50×0.15 = 50
        var score = ContractHealthScore.Create(
            ValidApiAssetId, 50, 50, 50, 50, 50, 50, 50, FixedDate);

        score.IsDegraded.Should().BeFalse();
        score.OverallScore.Should().Be(50);
    }

    // ── Valores nos limites (0 e 100) ────────────────────────────────

    [Fact]
    public void Create_Should_AcceptBoundaryValues_Zero()
    {
        var act = () => ContractHealthScore.Create(
            ValidApiAssetId, 0, 0, 0, 0, 0, 0, 0, FixedDate);

        act.Should().NotThrow();
    }

    [Fact]
    public void Create_Should_AcceptBoundaryValues_Hundred()
    {
        var act = () => ContractHealthScore.Create(
            ValidApiAssetId, 100, 100, 100, 100, 100, 100, 100, FixedDate);

        act.Should().NotThrow();
    }

    // ── Validação com valores inválidos ──────────────────────────────

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_Should_Throw_When_BreakingChangeFrequencyScoreOutOfRange(int invalidValue)
    {
        var act = () => ContractHealthScore.Create(
            ValidApiAssetId, invalidValue, 50, 50, 50, 50, 50, 50, FixedDate);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_Should_Throw_When_ConsumerImpactScoreOutOfRange(int invalidValue)
    {
        var act = () => ContractHealthScore.Create(
            ValidApiAssetId, 50, invalidValue, 50, 50, 50, 50, 50, FixedDate);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_Should_Throw_When_ReviewRecencyScoreOutOfRange(int invalidValue)
    {
        var act = () => ContractHealthScore.Create(
            ValidApiAssetId, 50, 50, invalidValue, 50, 50, 50, 50, FixedDate);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_Should_Throw_When_ExampleCoverageScoreOutOfRange(int invalidValue)
    {
        var act = () => ContractHealthScore.Create(
            ValidApiAssetId, 50, 50, 50, invalidValue, 50, 50, 50, FixedDate);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_Should_Throw_When_PolicyComplianceScoreOutOfRange(int invalidValue)
    {
        var act = () => ContractHealthScore.Create(
            ValidApiAssetId, 50, 50, 50, 50, invalidValue, 50, 50, FixedDate);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_Should_Throw_When_DocumentationScoreOutOfRange(int invalidValue)
    {
        var act = () => ContractHealthScore.Create(
            ValidApiAssetId, 50, 50, 50, 50, 50, invalidValue, 50, FixedDate);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_Should_Throw_When_DegradationThresholdOutOfRange(int invalidValue)
    {
        var act = () => ContractHealthScore.Create(
            ValidApiAssetId, 50, 50, 50, 50, 50, 50, invalidValue, FixedDate);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_Should_Throw_When_ApiAssetIdIsDefault()
    {
        var act = () => ContractHealthScore.Create(
            Guid.Empty, 50, 50, 50, 50, 50, 50, 50, FixedDate);

        act.Should().Throw<ArgumentException>();
    }

    // ── Recalculate ─────────────────────────────────────────────────

    [Fact]
    public void Recalculate_Should_UpdateAllDimensionsAndOverall()
    {
        var score = ContractHealthScore.Create(
            ValidApiAssetId, 50, 50, 50, 50, 50, 50, 50, FixedDate);

        var newDate = FixedDate.AddHours(1);
        score.Recalculate(90, 80, 70, 60, 95, 85, 40, newDate);

        score.BreakingChangeFrequencyScore.Should().Be(90);
        score.ConsumerImpactScore.Should().Be(80);
        score.ReviewRecencyScore.Should().Be(70);
        score.ExampleCoverageScore.Should().Be(60);
        score.PolicyComplianceScore.Should().Be(95);
        score.DocumentationScore.Should().Be(85);
        score.DegradationThreshold.Should().Be(40);
        score.CalculatedAt.Should().Be(newDate);

        // 90×0.20 + 80×0.20 + 70×0.15 + 60×0.15 + 95×0.15 + 85×0.15 = 18+16+10.5+9+14.25+12.75 = 80.5 → 80 (banker's rounding)
        score.OverallScore.Should().Be(80);
    }

    [Fact]
    public void Recalculate_Should_UpdateIsDegraded_When_ScoreDropsBelowThreshold()
    {
        var score = ContractHealthScore.Create(
            ValidApiAssetId, 80, 80, 80, 80, 80, 80, 50, FixedDate);

        score.IsDegraded.Should().BeFalse();

        score.Recalculate(10, 10, 10, 10, 10, 10, 50, FixedDate.AddHours(1));

        score.IsDegraded.Should().BeTrue();
        score.OverallScore.Should().Be(10);
    }

    [Fact]
    public void Recalculate_Should_ClearIsDegraded_When_ScoreRisesAboveThreshold()
    {
        var score = ContractHealthScore.Create(
            ValidApiAssetId, 10, 10, 10, 10, 10, 10, 50, FixedDate);

        score.IsDegraded.Should().BeTrue();

        score.Recalculate(90, 90, 90, 90, 90, 90, 50, FixedDate.AddHours(1));

        score.IsDegraded.Should().BeFalse();
        score.OverallScore.Should().Be(90);
    }

    [Fact]
    public void Recalculate_Should_Throw_When_InvalidValues()
    {
        var score = ContractHealthScore.Create(
            ValidApiAssetId, 50, 50, 50, 50, 50, 50, 50, FixedDate);

        var act = () => score.Recalculate(-1, 50, 50, 50, 50, 50, 50, FixedDate);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ── IDs únicos ──────────────────────────────────────────────────

    [Fact]
    public void Create_Should_GenerateUniqueIds()
    {
        var score1 = ContractHealthScore.Create(
            ValidApiAssetId, 50, 50, 50, 50, 50, 50, 50, FixedDate);
        var score2 = ContractHealthScore.Create(
            Guid.NewGuid(), 50, 50, 50, 50, 50, 50, 50, FixedDate);

        score1.Id.Should().NotBe(score2.Id);
    }

    // ── ContractHealthScoreId ───────────────────────────────────────

    [Fact]
    public void ContractHealthScoreId_New_Should_CreateUniqueId()
    {
        var id1 = ContractHealthScoreId.New();
        var id2 = ContractHealthScoreId.New();

        id1.Should().NotBe(id2);
        id1.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void ContractHealthScoreId_From_Should_PreserveGuid()
    {
        var guid = Guid.NewGuid();
        var id = ContractHealthScoreId.From(guid);

        id.Value.Should().Be(guid);
    }
}
