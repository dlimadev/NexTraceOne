using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

using RecordConfidenceEventFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RecordConfidenceEvent.RecordConfidenceEvent;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>Testes do handler RecordConfidenceEvent.</summary>
public sealed class RecordConfidenceEventTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static Release CreateRelease() =>
        Release.Create(Guid.NewGuid(), Guid.Empty, "TestService", "1.0.0", "prod", "https://ci/pipeline/1", "abc123def456", FixedNow);

    private static RecordConfidenceEventFeature.Handler CreateHandler(
        IReleaseRepository releaseRepository,
        IChangeConfidenceEventRepository confidenceEventRepository,
        IChangeIntelligenceUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
        => new(releaseRepository, confidenceEventRepository, unitOfWork, dateTimeProvider);

    [Fact]
    public async Task Handle_ShouldCreateEvent_WithDefaultConfidenceBefore_WhenNoEventsExist()
    {
        var release = CreateRelease();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var confRepo = Substitute.For<IChangeConfidenceEventRepository>();
        var uow = Substitute.For<IChangeIntelligenceUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);
        confRepo.GetLatestByReleaseAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((ChangeConfidenceEvent?)null);
        clock.UtcNow.Returns(FixedNow);

        var handler = CreateHandler(releaseRepo, confRepo, uow, clock);
        var command = new RecordConfidenceEventFeature.Command(
            release.Id.Value,
            ConfidenceEventType.Created,
            60,
            "Initial confidence",
            null,
            "system");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ConfidenceBefore.Should().Be(50); // default
        result.Value.ConfidenceAfter.Should().Be(60);
        result.Value.EventType.Should().Be(ConfidenceEventType.Created);
        result.Value.OccurredAt.Should().Be(FixedNow);

        await confRepo.Received(1).AddAsync(Arg.Any<ChangeConfidenceEvent>(), Arg.Any<CancellationToken>());
        await uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldUseLatestConfidenceAfter_AsConfidenceBefore()
    {
        var release = CreateRelease();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var confRepo = Substitute.For<IChangeConfidenceEventRepository>();
        var uow = Substitute.For<IChangeIntelligenceUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();

        var previousEvent = ChangeConfidenceEvent.Create(
            release.Id, ConfidenceEventType.Created, 50, 75,
            "Previous event", null, "system", FixedNow.AddHours(-1));

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);
        confRepo.GetLatestByReleaseAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(previousEvent);
        clock.UtcNow.Returns(FixedNow);

        var handler = CreateHandler(releaseRepo, confRepo, uow, clock);
        var command = new RecordConfidenceEventFeature.Command(
            release.Id.Value,
            ConfidenceEventType.StagingTested,
            85,
            "Staging tests passed",
            null,
            "ci-pipeline");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ConfidenceBefore.Should().Be(75); // from previous event
        result.Value.ConfidenceAfter.Should().Be(85);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenReleaseNotFound()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var confRepo = Substitute.For<IChangeConfidenceEventRepository>();
        var uow = Substitute.For<IChangeIntelligenceUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);

        var handler = CreateHandler(releaseRepo, confRepo, uow, clock);
        var command = new RecordConfidenceEventFeature.Command(
            Guid.NewGuid(),
            ConfidenceEventType.Deployed,
            80,
            "Deploy executed",
            null,
            "system");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");

        await confRepo.DidNotReceive().AddAsync(Arg.Any<ChangeConfidenceEvent>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_ShouldFail_WhenReleaseIdIsEmpty()
    {
        var validator = new RecordConfidenceEventFeature.Validator();
        var command = new RecordConfidenceEventFeature.Command(
            Guid.Empty,
            ConfidenceEventType.Created,
            60,
            "reason",
            null,
            "source");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReleaseId");
    }

    [Fact]
    public void Validator_ShouldFail_WhenConfidenceAfterOutOfRange()
    {
        var validator = new RecordConfidenceEventFeature.Validator();
        var command = new RecordConfidenceEventFeature.Command(
            Guid.NewGuid(),
            ConfidenceEventType.Created,
            101,
            "reason",
            null,
            "source");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConfidenceAfter");
    }

    [Fact]
    public void Validator_ShouldFail_WhenReasonIsEmpty()
    {
        var validator = new RecordConfidenceEventFeature.Validator();
        var command = new RecordConfidenceEventFeature.Command(
            Guid.NewGuid(),
            ConfidenceEventType.Created,
            60,
            "",
            null,
            "source");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public void Validator_ShouldFail_WhenSourceIsEmpty()
    {
        var validator = new RecordConfidenceEventFeature.Validator();
        var command = new RecordConfidenceEventFeature.Command(
            Guid.NewGuid(),
            ConfidenceEventType.Created,
            60,
            "reason",
            null,
            "");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Source");
    }

    [Fact]
    public void Validator_ShouldPass_WhenAllFieldsValid()
    {
        var validator = new RecordConfidenceEventFeature.Validator();
        var command = new RecordConfidenceEventFeature.Command(
            Guid.NewGuid(),
            ConfidenceEventType.Deployed,
            80,
            "Deploy successful",
            """{"env":"prod"}""",
            "ci-pipeline");

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
