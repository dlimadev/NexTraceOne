using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeIntelligence.Application.Abstractions;
using NexTraceOne.ChangeIntelligence.Domain.Entities;
using NSubstitute.ReturnsExtensions;
using GetChangeAdvisoryFeature = NexTraceOne.ChangeIntelligence.Application.Features.GetChangeAdvisory.GetChangeAdvisory;
using RecordChangeDecisionFeature = NexTraceOne.ChangeIntelligence.Application.Features.RecordChangeDecision.RecordChangeDecision;
using GetChangeDecisionHistoryFeature = NexTraceOne.ChangeIntelligence.Application.Features.GetChangeDecisionHistory.GetChangeDecisionHistory;

namespace NexTraceOne.ChangeIntelligence.Tests.Application.Features;

/// <summary>Testes dos handlers de Change Governance Decision (Advisory, Decision, History).</summary>
public sealed class ChangeGovernanceDecisionTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static Release CreateRelease(string serviceName = "TestService") =>
        Release.Create(Guid.NewGuid(), serviceName, "1.0.0", "prod", "https://ci/pipeline/1", "abc123def456", FixedNow);

    // ── GetChangeAdvisory ───────────────────────────────────────────────

    [Fact]
    public async Task GetChangeAdvisory_Should_ReturnAdvisory_WhenReleaseExists()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var scoreRepo = Substitute.For<IChangeScoreRepository>();
        var blastRepo = Substitute.For<IBlastRadiusRepository>();
        var rollbackRepo = Substitute.For<IRollbackAssessmentRepository>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var release = CreateRelease();
        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);
        scoreRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .ReturnsNull();
        blastRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .ReturnsNull();
        rollbackRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var sut = new GetChangeAdvisoryFeature.Handler(releaseRepo, scoreRepo, blastRepo, rollbackRepo, dateTimeProvider);

        var result = await sut.Handle(
            new GetChangeAdvisoryFeature.Query(release.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReleaseId.Should().Be(release.Id.Value);
        result.Value.Recommendation.Should().NotBeNullOrEmpty();
        result.Value.Factors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetChangeAdvisory_Should_ReturnError_WhenReleaseNotFound()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var scoreRepo = Substitute.For<IChangeScoreRepository>();
        var blastRepo = Substitute.For<IBlastRadiusRepository>();
        var rollbackRepo = Substitute.For<IRollbackAssessmentRepository>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var sut = new GetChangeAdvisoryFeature.Handler(releaseRepo, scoreRepo, blastRepo, rollbackRepo, dateTimeProvider);

        var result = await sut.Handle(
            new GetChangeAdvisoryFeature.Query(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetChangeAdvisory_Should_RecommendApprove_WhenScoreLowAndAllFactorsPass()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var scoreRepo = Substitute.For<IChangeScoreRepository>();
        var blastRepo = Substitute.For<IBlastRadiusRepository>();
        var rollbackRepo = Substitute.For<IRollbackAssessmentRepository>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var release = CreateRelease();
        release.SetChangeScore(0.1m);

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);

        var score = ChangeIntelligenceScore.Compute(release.Id, 0.1m, 0.1m, 0.1m, FixedNow);
        scoreRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(score);

        var blast = BlastRadiusReport.Calculate(release.Id, Guid.NewGuid(), [], [], FixedNow);
        blastRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(blast);

        var rollback = RollbackAssessment.Create(
            release.Id, true, 0.9m, "0.9.0", true, 0, 0, null, "safe to rollback", FixedNow);
        rollbackRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(rollback);

        var sut = new GetChangeAdvisoryFeature.Handler(releaseRepo, scoreRepo, blastRepo, rollbackRepo, dateTimeProvider);

        var result = await sut.Handle(
            new GetChangeAdvisoryFeature.Query(release.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Recommendation.Should().Be("Approve");
    }

    [Fact]
    public async Task GetChangeAdvisory_Should_RecommendNeedsMoreEvidence_WhenManyFactorsUnknown()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var scoreRepo = Substitute.For<IChangeScoreRepository>();
        var blastRepo = Substitute.For<IBlastRadiusRepository>();
        var rollbackRepo = Substitute.For<IRollbackAssessmentRepository>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var release = CreateRelease();
        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);

        scoreRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .ReturnsNull();
        blastRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .ReturnsNull();
        rollbackRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var sut = new GetChangeAdvisoryFeature.Handler(releaseRepo, scoreRepo, blastRepo, rollbackRepo, dateTimeProvider);

        var result = await sut.Handle(
            new GetChangeAdvisoryFeature.Query(release.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Recommendation.Should().Be("NeedsMoreEvidence");
    }

    [Fact]
    public void GetChangeAdvisory_Validator_Should_RejectEmptyReleaseId()
    {
        var validator = new GetChangeAdvisoryFeature.Validator();
        var query = new GetChangeAdvisoryFeature.Query(Guid.Empty);

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReleaseId");
    }

    // ── RecordChangeDecision ────────────────────────────────────────────

    [Fact]
    public async Task RecordChangeDecision_Should_RecordDecision_WhenReleaseExists()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var changeEventRepo = Substitute.For<IChangeEventRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var release = CreateRelease();
        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);

        var sut = new RecordChangeDecisionFeature.Handler(releaseRepo, changeEventRepo, unitOfWork, dateTimeProvider);

        var result = await sut.Handle(
            new RecordChangeDecisionFeature.Command(
                release.Id.Value, "Approved", "admin@test.com", "All checks passed", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Decision.Should().Be("Approved");
        result.Value.ReleaseId.Should().Be(release.Id.Value);
        result.Value.DecidedBy.Should().Be("admin@test.com");
        changeEventRepo.Received(1).Add(Arg.Any<ChangeEvent>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordChangeDecision_Should_ReturnError_WhenReleaseNotFound()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var changeEventRepo = Substitute.For<IChangeEventRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var sut = new RecordChangeDecisionFeature.Handler(releaseRepo, changeEventRepo, unitOfWork, dateTimeProvider);

        var result = await sut.Handle(
            new RecordChangeDecisionFeature.Command(
                Guid.NewGuid(), "Approved", "admin@test.com", "All checks passed", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void RecordChangeDecision_Validator_Should_RejectEmptyDecision()
    {
        var validator = new RecordChangeDecisionFeature.Validator();
        var command = new RecordChangeDecisionFeature.Command(
            Guid.NewGuid(), "", "admin@test.com", "Some rationale", null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Decision");
    }

    [Theory]
    [InlineData("InvalidStatus")]
    [InlineData("Pending")]
    [InlineData("approved")]
    public void RecordChangeDecision_Validator_Should_RejectInvalidDecisionValues(string decision)
    {
        var validator = new RecordChangeDecisionFeature.Validator();
        var command = new RecordChangeDecisionFeature.Command(
            Guid.NewGuid(), decision, "admin@test.com", "Some rationale", null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Decision");
    }

    [Fact]
    public void RecordChangeDecision_Validator_Should_RejectEmptyRationale()
    {
        var validator = new RecordChangeDecisionFeature.Validator();
        var command = new RecordChangeDecisionFeature.Command(
            Guid.NewGuid(), "Approved", "admin@test.com", "", null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Rationale");
    }

    // ── GetChangeDecisionHistory ────────────────────────────────────────

    [Fact]
    public async Task GetChangeDecisionHistory_Should_ReturnHistory_WhenReleaseExists()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var changeEventRepo = Substitute.For<IChangeEventRepository>();

        var release = CreateRelease();
        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);

        var events = new List<ChangeEvent>
        {
            ChangeEvent.Create(release.Id, "governance_decision_approved", "Decision: Approved", "admin@test.com", FixedNow),
            ChangeEvent.Create(release.Id, "governance_decision_rejected", "Decision: Rejected", "reviewer@test.com", FixedNow.AddHours(1))
        };

        changeEventRepo.ListByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(events);

        var sut = new GetChangeDecisionHistoryFeature.Handler(releaseRepo, changeEventRepo);

        var result = await sut.Handle(
            new GetChangeDecisionHistoryFeature.Query(release.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReleaseId.Should().Be(release.Id.Value);
        result.Value.Decisions.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetChangeDecisionHistory_Should_ReturnError_WhenReleaseNotFound()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var changeEventRepo = Substitute.For<IChangeEventRepository>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .ReturnsNull();

        var sut = new GetChangeDecisionHistoryFeature.Handler(releaseRepo, changeEventRepo);

        var result = await sut.Handle(
            new GetChangeDecisionHistoryFeature.Query(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetChangeDecisionHistory_Should_ReturnEmptyList_WhenNoEventsExist()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var changeEventRepo = Substitute.For<IChangeEventRepository>();

        var release = CreateRelease();
        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);

        changeEventRepo.ListByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(new List<ChangeEvent>());

        var sut = new GetChangeDecisionHistoryFeature.Handler(releaseRepo, changeEventRepo);

        var result = await sut.Handle(
            new GetChangeDecisionHistoryFeature.Query(release.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Decisions.Should().BeEmpty();
    }

    [Fact]
    public void GetChangeDecisionHistory_Validator_Should_RejectEmptyReleaseId()
    {
        var validator = new GetChangeDecisionHistoryFeature.Validator();
        var query = new GetChangeDecisionHistoryFeature.Query(Guid.Empty);

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReleaseId");
    }
}
