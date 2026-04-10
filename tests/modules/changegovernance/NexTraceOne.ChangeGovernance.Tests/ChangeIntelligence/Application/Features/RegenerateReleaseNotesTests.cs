using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

using RegenerateReleaseNotesFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RegenerateReleaseNotes.RegenerateReleaseNotes;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>Testes do handler RegenerateReleaseNotes.</summary>
public sealed class RegenerateReleaseNotesTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static Release CreateRelease() =>
        Release.Create(Guid.NewGuid(), "TestService", "1.0.0", "prod", "https://ci/pipeline/1", "abc123def456", FixedNow);

    private static RegenerateReleaseNotesFeature.Handler CreateHandler(
        IReleaseRepository releaseRepository,
        IReleaseNotesRepository releaseNotesRepository,
        IDateTimeProvider dateTimeProvider)
        => new(releaseRepository, releaseNotesRepository, dateTimeProvider);

    [Fact]
    public async Task Handle_ShouldRegenerateNotes_WhenBothExist()
    {
        var release = CreateRelease();
        var existingNotes = ReleaseNotes.Create(
            ReleaseNotesId.New(),
            release.Id,
            "Old summary",
            null, null, null, null, null, null,
            "template-v1",
            tokensUsed: 0,
            ReleaseNotesStatus.Published,
            null,
            FixedNow);

        var releaseRepo = Substitute.For<IReleaseRepository>();
        var notesRepo = Substitute.For<IReleaseNotesRepository>();
        var clock = Substitute.For<IDateTimeProvider>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);
        notesRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(existingNotes);
        clock.UtcNow.Returns(FixedNow.AddHours(1));

        var handler = CreateHandler(releaseRepo, notesRepo, clock);
        var command = new RegenerateReleaseNotesFeature.Command(release.Id.Value);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TechnicalSummary.Should().Contain("TestService");
        result.Value.RegenerationCount.Should().Be(1);
        result.Value.RegeneratedAt.Should().Be(FixedNow.AddHours(1));

        notesRepo.Received(1).Update(Arg.Any<ReleaseNotes>());
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenReleaseNotFound()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var notesRepo = Substitute.For<IReleaseNotesRepository>();
        var clock = Substitute.For<IDateTimeProvider>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);

        var handler = CreateHandler(releaseRepo, notesRepo, clock);
        var command = new RegenerateReleaseNotesFeature.Command(Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenNotesNotFound()
    {
        var release = CreateRelease();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var notesRepo = Substitute.For<IReleaseNotesRepository>();
        var clock = Substitute.For<IDateTimeProvider>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);
        notesRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((ReleaseNotes?)null);

        var handler = CreateHandler(releaseRepo, notesRepo, clock);
        var command = new RegenerateReleaseNotesFeature.Command(release.Id.Value);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("ReleaseNotes.NotFound");
    }

    [Fact]
    public void Validator_ShouldFail_WhenReleaseIdIsEmpty()
    {
        var validator = new RegenerateReleaseNotesFeature.Validator();
        var command = new RegenerateReleaseNotesFeature.Command(Guid.Empty);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReleaseId");
    }

    [Fact]
    public void Validator_ShouldPass_WhenReleaseIdIsValid()
    {
        var validator = new RegenerateReleaseNotesFeature.Validator();
        var command = new RegenerateReleaseNotesFeature.Command(Guid.NewGuid());

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
