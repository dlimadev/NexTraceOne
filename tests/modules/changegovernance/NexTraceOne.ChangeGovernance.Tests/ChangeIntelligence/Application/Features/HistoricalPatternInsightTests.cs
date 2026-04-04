using System.Linq;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

using GetHistoricalPatternInsightFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetHistoricalPatternInsight.GetHistoricalPatternInsight;
using GetChangeAdvisoryFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeAdvisory.GetChangeAdvisory;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>
/// Testes do padrão histórico de releases para Change Confidence Score V2.
/// Cobre: cálculo de métricas, derivação de PatternRisk e integração no Advisory.
/// </summary>
public sealed class HistoricalPatternInsightTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 4, 4, 12, 0, 0, TimeSpan.Zero);

    private static Release CreateRelease(
        string serviceName = "svc-payments",
        string environment = "production",
        DeploymentStatus status = DeploymentStatus.Succeeded,
        ChangeLevel changeLevel = ChangeLevel.Breaking)
    {
        var r = Release.Create(
            Guid.NewGuid(), serviceName, "1.0.0", environment,
            "https://ci/pipeline", "abc123", FixedNow.AddDays(-10));
        r.Classify(changeLevel);
        if (status == DeploymentStatus.Succeeded)
        {
            r.UpdateStatus(DeploymentStatus.Running);
            r.UpdateStatus(DeploymentStatus.Succeeded);
        }
        else if (status == DeploymentStatus.Failed)
        {
            r.UpdateStatus(DeploymentStatus.Running);
            r.UpdateStatus(DeploymentStatus.Failed);
        }
        else if (status == DeploymentStatus.RolledBack)
        {
            r.UpdateStatus(DeploymentStatus.Running);
            r.UpdateStatus(DeploymentStatus.Succeeded);
            r.RegisterRollback(ReleaseId.New());
        }
        return r;
    }

    // ── GetHistoricalPatternInsight — happy path ─────────────────────────────

    [Fact]
    public async Task Handler_WhenSufficientSamplesWithHighAdverseRate_ShouldReturnHighRisk()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var targetRelease = Release.Create(
            Guid.NewGuid(), "svc-payments", "2.0.0", "production",
            "https://ci/pipeline", "def456", FixedNow);
        targetRelease.Classify(ChangeLevel.Breaking);

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(targetRelease);

        // 7 similar releases: 4 rollbacks + 2 failures + 1 success = adverse rate 85%
        var similar = new List<Release>
        {
            CreateRelease(status: DeploymentStatus.RolledBack),
            CreateRelease(status: DeploymentStatus.RolledBack),
            CreateRelease(status: DeploymentStatus.RolledBack),
            CreateRelease(status: DeploymentStatus.RolledBack),
            CreateRelease(status: DeploymentStatus.Failed),
            CreateRelease(status: DeploymentStatus.Failed),
            CreateRelease(status: DeploymentStatus.Succeeded),
        };

        releaseRepo.ListSimilarReleasesAsync(
            Arg.Any<ReleaseId>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<ChangeLevel>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(similar);

        var sut = new GetHistoricalPatternInsightFeature.Handler(releaseRepo, clock);
        var result = await sut.Handle(
            new GetHistoricalPatternInsightFeature.Query(targetRelease.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PatternRisk.Should().Be("High");
        result.Value.TotalSamples.Should().Be(7);
        result.Value.RollbackRate.Should().BeApproximately(4m / 7m, 0.001m);
        result.Value.FailureRate.Should().BeApproximately(2m / 7m, 0.001m);
        result.Value.PatternRationale.Should().Contain("elevated risk");
    }

    [Fact]
    public async Task Handler_WhenSufficientSamplesWithModerateAdverseRate_ShouldReturnModerateRisk()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var targetRelease = Release.Create(
            Guid.NewGuid(), "svc-orders", "1.5.0", "staging",
            "https://ci/pipeline", "ghi789", FixedNow);
        targetRelease.Classify(ChangeLevel.Additive);

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(targetRelease);

        // 8 similar: 2 rollbacks + 6 successes = adverse rate 25%
        var similar = Enumerable.Range(0, 6)
            .Select(_ => CreateRelease(status: DeploymentStatus.Succeeded,
                changeLevel: ChangeLevel.Additive))
            .Concat(new[]
            {
                CreateRelease(status: DeploymentStatus.RolledBack, changeLevel: ChangeLevel.Additive),
                CreateRelease(status: DeploymentStatus.RolledBack, changeLevel: ChangeLevel.Additive),
            })
            .ToList();

        releaseRepo.ListSimilarReleasesAsync(
            Arg.Any<ReleaseId>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<ChangeLevel>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(similar);

        var sut = new GetHistoricalPatternInsightFeature.Handler(releaseRepo, clock);
        var result = await sut.Handle(
            new GetHistoricalPatternInsightFeature.Query(targetRelease.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PatternRisk.Should().Be("Moderate");
        result.Value.TotalSamples.Should().Be(8);
        result.Value.PatternRationale.Should().Contain("moderate risk");
    }

    [Fact]
    public async Task Handler_WhenAllSampleSucceeded_ShouldReturnLowRisk()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var targetRelease = Release.Create(
            Guid.NewGuid(), "svc-catalog", "3.0.0", "staging",
            "https://ci/pipeline", "jkl012", FixedNow);
        targetRelease.Classify(ChangeLevel.NonBreaking);

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(targetRelease);

        var similar = Enumerable.Range(0, 10)
            .Select(_ => CreateRelease(status: DeploymentStatus.Succeeded,
                changeLevel: ChangeLevel.NonBreaking))
            .ToList();

        releaseRepo.ListSimilarReleasesAsync(
            Arg.Any<ReleaseId>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<ChangeLevel>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(similar);

        var sut = new GetHistoricalPatternInsightFeature.Handler(releaseRepo, clock);
        var result = await sut.Handle(
            new GetHistoricalPatternInsightFeature.Query(targetRelease.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PatternRisk.Should().Be("Low");
        result.Value.SuccessRate.Should().Be(1.0m);
        result.Value.PatternRationale.Should().Contain("low deployment risk");
    }

    [Fact]
    public async Task Handler_WhenInsufficientSamples_ShouldReturnInsufficientRisk()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var targetRelease = Release.Create(
            Guid.NewGuid(), "svc-new", "1.0.0", "production",
            "https://ci/pipeline", "mno345", FixedNow);
        targetRelease.Classify(ChangeLevel.Breaking);

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(targetRelease);

        // Only 2 samples — below the minimum of 5
        releaseRepo.ListSimilarReleasesAsync(
            Arg.Any<ReleaseId>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<ChangeLevel>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<Release>
            {
                CreateRelease(status: DeploymentStatus.Succeeded),
                CreateRelease(status: DeploymentStatus.Succeeded),
            });

        var sut = new GetHistoricalPatternInsightFeature.Handler(releaseRepo, clock);
        var result = await sut.Handle(
            new GetHistoricalPatternInsightFeature.Query(targetRelease.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PatternRisk.Should().Be("Insufficient");
        result.Value.PatternRationale.Should().Contain("insufficient data");
    }

    [Fact]
    public async Task Handler_WhenNoSamples_ShouldReturnInsufficientRisk()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var targetRelease = Release.Create(
            Guid.NewGuid(), "svc-brand-new", "0.1.0", "production",
            "https://ci/pipeline", "pqr678", FixedNow);

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(targetRelease);

        releaseRepo.ListSimilarReleasesAsync(
            Arg.Any<ReleaseId>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<ChangeLevel>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<Release>());

        var sut = new GetHistoricalPatternInsightFeature.Handler(releaseRepo, clock);
        var result = await sut.Handle(
            new GetHistoricalPatternInsightFeature.Query(targetRelease.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PatternRisk.Should().Be("Insufficient");
        result.Value.TotalSamples.Should().Be(0);
    }

    [Fact]
    public async Task Handler_WhenReleaseNotFound_ShouldReturnFailure()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);

        var sut = new GetHistoricalPatternInsightFeature.Handler(releaseRepo, clock);
        var result = await sut.Handle(
            new GetHistoricalPatternInsightFeature.Query(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(50)]
    public async Task Handler_ShouldRespectCustomLookbackDays(int lookbackDays)
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var targetRelease = Release.Create(
            Guid.NewGuid(), "svc-test", "1.0.0", "staging",
            "https://ci/pipeline", "stu901", FixedNow);

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(targetRelease);

        releaseRepo.ListSimilarReleasesAsync(
            Arg.Any<ReleaseId>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<ChangeLevel>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<Release>());

        var effectiveLookback = lookbackDays > 0 ? lookbackDays : 90; // 0 uses default
        var sut = new GetHistoricalPatternInsightFeature.Handler(releaseRepo, clock);
        var result = await sut.Handle(
            new GetHistoricalPatternInsightFeature.Query(
                targetRelease.Id.Value,
                lookbackDays > 0 ? lookbackDays : null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.LookbackDays.Should().Be(effectiveLookback);
    }

    // ── GetChangeAdvisory — HistoricalPattern factor ─────────────────────────

    [Fact]
    public async Task Advisory_WhenHighHistoricalAdverseRate_ShouldIncludeHistoricalPatternFactor()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var scoreRepo = Substitute.For<IChangeScoreRepository>();
        var blastRepo = Substitute.For<IBlastRadiusRepository>();
        var rollbackRepo = Substitute.For<IRollbackAssessmentRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var targetRelease = Release.Create(
            Guid.NewGuid(), "svc-core", "4.0.0", "production",
            "https://ci/pipeline", "vwx234", FixedNow);
        targetRelease.Classify(ChangeLevel.Breaking);

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(targetRelease);

        // 6 adverse out of 8 = 75% adverse rate
        var similar = new List<Release>
        {
            CreateRelease(status: DeploymentStatus.RolledBack),
            CreateRelease(status: DeploymentStatus.RolledBack),
            CreateRelease(status: DeploymentStatus.RolledBack),
            CreateRelease(status: DeploymentStatus.Failed),
            CreateRelease(status: DeploymentStatus.Failed),
            CreateRelease(status: DeploymentStatus.Failed),
            CreateRelease(status: DeploymentStatus.Succeeded),
            CreateRelease(status: DeploymentStatus.Succeeded),
        };

        releaseRepo.ListSimilarReleasesAsync(
            Arg.Any<ReleaseId>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<ChangeLevel>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(similar);

        scoreRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((ChangeIntelligenceScore?)null);
        blastRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((BlastRadiusReport?)null);
        rollbackRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((RollbackAssessment?)null);

        var sut = new GetChangeAdvisoryFeature.Handler(
            releaseRepo, scoreRepo, blastRepo, rollbackRepo, clock);

        var result = await sut.Handle(
            new GetChangeAdvisoryFeature.Query(targetRelease.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var historicalFactor = result.Value.Factors
            .SingleOrDefault(f => f.FactorName == "HistoricalPattern");

        historicalFactor.Should().NotBeNull("HistoricalPattern must be a factor in the advisory");
        historicalFactor!.Status.Should().Be("Fail",
            "75% adverse rate should produce Fail status in the advisory");
    }

    [Fact]
    public async Task Advisory_ShouldHaveFiveFactors_WithHistoricalPatternAsLast()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var scoreRepo = Substitute.For<IChangeScoreRepository>();
        var blastRepo = Substitute.For<IBlastRadiusRepository>();
        var rollbackRepo = Substitute.For<IRollbackAssessmentRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var targetRelease = Release.Create(
            Guid.NewGuid(), "svc-test", "1.0.0", "staging",
            "https://ci/pipeline", "yza567", FixedNow);

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(targetRelease);
        releaseRepo.ListSimilarReleasesAsync(
            Arg.Any<ReleaseId>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<ChangeLevel>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<Release>());

        scoreRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((ChangeIntelligenceScore?)null);
        blastRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((BlastRadiusReport?)null);
        rollbackRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((RollbackAssessment?)null);

        var sut = new GetChangeAdvisoryFeature.Handler(
            releaseRepo, scoreRepo, blastRepo, rollbackRepo, clock);

        var result = await sut.Handle(
            new GetChangeAdvisoryFeature.Query(targetRelease.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Factors.Should().HaveCount(5, "advisory must have 5 factors");
        result.Value.Factors.Last().FactorName.Should().Be("HistoricalPattern",
            "HistoricalPattern should be the last (5th) factor");
    }

    [Fact]
    public async Task Advisory_AllWeightsShouldSumToOne_WithFiveFactors()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var scoreRepo = Substitute.For<IChangeScoreRepository>();
        var blastRepo = Substitute.For<IBlastRadiusRepository>();
        var rollbackRepo = Substitute.For<IRollbackAssessmentRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var targetRelease = Release.Create(
            Guid.NewGuid(), "svc-test", "1.0.0", "production",
            "https://ci/pipeline", "bcd890", FixedNow);

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(targetRelease);
        releaseRepo.ListSimilarReleasesAsync(
            Arg.Any<ReleaseId>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<ChangeLevel>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<Release>());
        scoreRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((ChangeIntelligenceScore?)null);
        blastRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((BlastRadiusReport?)null);
        rollbackRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((RollbackAssessment?)null);

        var sut = new GetChangeAdvisoryFeature.Handler(
            releaseRepo, scoreRepo, blastRepo, rollbackRepo, clock);

        var result = await sut.Handle(
            new GetChangeAdvisoryFeature.Query(targetRelease.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var totalWeight = result.Value.Factors.Sum(f => f.Weight ?? 0m);
        totalWeight.Should().Be(1.0m,
            "the 5 advisory factors must have equal weights that sum to 1.0 (5 × 0.20)");
    }
}
