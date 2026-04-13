using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

using GetChangeConfidenceTimelineFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeConfidenceTimeline.GetChangeConfidenceTimeline;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>Testes do handler GetChangeConfidenceTimeline.</summary>
public sealed class GetChangeConfidenceTimelineTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static Release CreateRelease() =>
        Release.Create(Guid.NewGuid(), Guid.Empty, "TestService", "1.0.0", "prod", "https://ci/pipeline/1", "abc123def456", FixedNow);

    [Fact]
    public async Task Handle_ShouldReturnOrderedTimeline()
    {
        var release = CreateRelease();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var confRepo = Substitute.For<IChangeConfidenceEventRepository>();

        var evt1 = ChangeConfidenceEvent.Create(
            release.Id, ConfidenceEventType.Created, 50, 60,
            "Initial", null, "system", FixedNow);
        var evt2 = ChangeConfidenceEvent.Create(
            release.Id, ConfidenceEventType.StagingTested, 60, 80,
            "Staging passed", null, "ci", FixedNow.AddHours(1));
        var evt3 = ChangeConfidenceEvent.Create(
            release.Id, ConfidenceEventType.Deployed, 80, 85,
            "Deployed", null, "ci", FixedNow.AddHours(2));

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);
        confRepo.ListByReleaseAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(new List<ChangeConfidenceEvent> { evt1, evt2, evt3 });

        var handler = new GetChangeConfidenceTimelineFeature.Handler(releaseRepo, confRepo);
        var query = new GetChangeConfidenceTimelineFeature.Query(release.Id.Value);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReleaseId.Should().Be(release.Id.Value);
        result.Value.CurrentConfidence.Should().Be(85);
        result.Value.Events.Should().HaveCount(3);
        result.Value.Events[0].EventType.Should().Be(ConfidenceEventType.Created);
        result.Value.Events[1].EventType.Should().Be(ConfidenceEventType.StagingTested);
        result.Value.Events[2].EventType.Should().Be(ConfidenceEventType.Deployed);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenNoEvents()
    {
        var release = CreateRelease();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var confRepo = Substitute.For<IChangeConfidenceEventRepository>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);
        confRepo.ListByReleaseAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(new List<ChangeConfidenceEvent>());

        var handler = new GetChangeConfidenceTimelineFeature.Handler(releaseRepo, confRepo);
        var query = new GetChangeConfidenceTimelineFeature.Query(release.Id.Value);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Events.Should().BeEmpty();
        result.Value.CurrentConfidence.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenReleaseNotFound()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var confRepo = Substitute.For<IChangeConfidenceEventRepository>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);

        var handler = new GetChangeConfidenceTimelineFeature.Handler(releaseRepo, confRepo);
        var query = new GetChangeConfidenceTimelineFeature.Query(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public void Validator_ShouldFail_WhenReleaseIdIsEmpty()
    {
        var validator = new GetChangeConfidenceTimelineFeature.Validator();
        var query = new GetChangeConfidenceTimelineFeature.Query(Guid.Empty);

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReleaseId");
    }

    [Fact]
    public void Validator_ShouldPass_WhenReleaseIdIsValid()
    {
        var validator = new GetChangeConfidenceTimelineFeature.Validator();
        var query = new GetChangeConfidenceTimelineFeature.Query(Guid.NewGuid());

        var result = validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnCurrentConfidence_FromLastEvent()
    {
        var release = CreateRelease();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var confRepo = Substitute.For<IChangeConfidenceEventRepository>();

        var evt = ChangeConfidenceEvent.Create(
            release.Id, ConfidenceEventType.PostDeployValidated, 80, 95,
            "Post-deploy OK", null, "system", FixedNow);

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);
        confRepo.ListByReleaseAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(new List<ChangeConfidenceEvent> { evt });

        var handler = new GetChangeConfidenceTimelineFeature.Handler(releaseRepo, confRepo);
        var query = new GetChangeConfidenceTimelineFeature.Query(release.Id.Value);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentConfidence.Should().Be(95);
        result.Value.Events.Should().HaveCount(1);
    }
}
