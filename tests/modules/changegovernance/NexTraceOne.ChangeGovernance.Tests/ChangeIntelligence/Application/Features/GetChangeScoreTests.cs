using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

using GetChangeScoreFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeScore.GetChangeScore;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes unitários para GetChangeScore — consulta o score de risco computado de uma Release.
/// Valida happy path, caso de score ausente e validação de entrada.
/// </summary>
public sealed class GetChangeScoreTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 20, 12, 0, 0, TimeSpan.Zero);

    private readonly IChangeScoreRepository _scoreRepo = Substitute.For<IChangeScoreRepository>();

    private GetChangeScoreFeature.Handler CreateHandler() => new(_scoreRepo);

    private static ChangeIntelligenceScore MakeScore(ReleaseId releaseId) =>
        ChangeIntelligenceScore.Compute(
            releaseId,
            breakingChangeWeight: 0.8m,
            blastRadiusWeight: 0.5m,
            environmentWeight: 0.9m,
            computedAt: FixedNow,
            scoreSource: "auto:unit-test");

    // ── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ExistingScore_ReturnsExpectedFields()
    {
        var releaseId = ReleaseId.New();
        var score = MakeScore(releaseId);

        _scoreRepo.GetByReleaseIdAsync(releaseId, Arg.Any<CancellationToken>())
            .Returns(score);

        var result = await CreateHandler().Handle(
            new GetChangeScoreFeature.Query(releaseId.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReleaseId.Should().Be(releaseId.Value);
        result.Value.Score.Should().BeInRange(0m, 1m);
        result.Value.BreakingChangeWeight.Should().Be(0.8m);
        result.Value.BlastRadiusWeight.Should().Be(0.5m);
        result.Value.EnvironmentWeight.Should().Be(0.9m);
        result.Value.ScoreSource.Should().Be("auto:unit-test");
        result.Value.ComputedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task Handle_ScoreIsWeightedAverage_OfThreeFactors()
    {
        var releaseId = ReleaseId.New();
        var score = MakeScore(releaseId);

        _scoreRepo.GetByReleaseIdAsync(releaseId, Arg.Any<CancellationToken>())
            .Returns(score);

        var result = await CreateHandler().Handle(
            new GetChangeScoreFeature.Query(releaseId.Value),
            CancellationToken.None);

        var expected = Math.Round((0.8m + 0.5m + 0.9m) / 3m, 4);
        result.Value.Score.Should().Be(expected);
    }

    // ── Not found ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ScoreNotFound_ReturnsFailure()
    {
        var releaseId = ReleaseId.New();

        _scoreRepo.GetByReleaseIdAsync(releaseId, Arg.Any<CancellationToken>())
            .Returns((ChangeIntelligenceScore?)null);

        var result = await CreateHandler().Handle(
            new GetChangeScoreFeature.Query(releaseId.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    // ── Validator ────────────────────────────────────────────────────────────

    [Fact]
    public void Validator_EmptyReleaseId_ReturnsError()
    {
        var validator = new GetChangeScoreFeature.Validator();
        var result = validator.Validate(new GetChangeScoreFeature.Query(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "ReleaseId");
    }

    [Fact]
    public void Validator_ValidReleaseId_Passes()
    {
        var validator = new GetChangeScoreFeature.Validator();
        var result = validator.Validate(new GetChangeScoreFeature.Query(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }
}
