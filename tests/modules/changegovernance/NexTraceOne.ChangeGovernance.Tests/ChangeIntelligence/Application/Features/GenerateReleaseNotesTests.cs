using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

using GenerateReleaseNotesFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GenerateReleaseNotes.GenerateReleaseNotes;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>Testes do handler GenerateReleaseNotes.</summary>
public sealed class GenerateReleaseNotesTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static Release CreateRelease() =>
        Release.Create(Guid.NewGuid(), "TestService", "1.0.0", "prod", "https://ci/pipeline/1", "abc123def456", FixedNow);

    private static GenerateReleaseNotesFeature.Handler CreateHandler(
        IReleaseRepository releaseRepository,
        IReleaseNotesRepository releaseNotesRepository,
        IDateTimeProvider dateTimeProvider,
        ICurrentTenant currentTenant)
        => new(releaseRepository, releaseNotesRepository, dateTimeProvider, currentTenant);

    [Fact]
    public async Task Handle_ShouldGenerateReleaseNotes_WhenReleaseExists()
    {
        var release = CreateRelease();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var notesRepo = Substitute.For<IReleaseNotesRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        var tenant = Substitute.For<ICurrentTenant>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);
        notesRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((ReleaseNotes?)null);
        clock.UtcNow.Returns(FixedNow);
        tenant.IsActive.Returns(true);
        tenant.Id.Returns(Guid.NewGuid());

        var handler = CreateHandler(releaseRepo, notesRepo, clock, tenant);
        var command = new GenerateReleaseNotesFeature.Command(release.Id.Value, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TechnicalSummary.Should().Contain("TestService");
        result.Value.TechnicalSummary.Should().Contain("1.0.0");
        result.Value.ExecutiveSummary.Should().BeNull();
        result.Value.ModelUsed.Should().Be("template-v1");
        result.Value.GeneratedAt.Should().Be(FixedNow);

        await notesRepo.Received(1).AddAsync(Arg.Any<ReleaseNotes>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldGenerateExecutiveSummary_WhenPersonaModeIsExecutive()
    {
        var release = CreateRelease();
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var notesRepo = Substitute.For<IReleaseNotesRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        var tenant = Substitute.For<ICurrentTenant>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);
        notesRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((ReleaseNotes?)null);
        clock.UtcNow.Returns(FixedNow);
        tenant.IsActive.Returns(false);

        var handler = CreateHandler(releaseRepo, notesRepo, clock, tenant);
        var command = new GenerateReleaseNotesFeature.Command(release.Id.Value, "executive");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExecutiveSummary.Should().NotBeNull();
        result.Value.ExecutiveSummary.Should().Contain("TestService");
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenReleaseNotFound()
    {
        var releaseRepo = Substitute.For<IReleaseRepository>();
        var notesRepo = Substitute.For<IReleaseNotesRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        var tenant = Substitute.For<ICurrentTenant>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);

        var handler = CreateHandler(releaseRepo, notesRepo, clock, tenant);
        var command = new GenerateReleaseNotesFeature.Command(Guid.NewGuid(), null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");

        await notesRepo.DidNotReceive().AddAsync(Arg.Any<ReleaseNotes>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenReleaseNotesAlreadyExist()
    {
        var release = CreateRelease();
        var existingNotes = ReleaseNotes.Create(
            ReleaseNotesId.New(),
            release.Id,
            "Existing summary",
            null, null, null, null, null, null,
            "template-v1",
            tokensUsed: 0,
            ReleaseNotesStatus.Draft,
            null,
            FixedNow);

        var releaseRepo = Substitute.For<IReleaseRepository>();
        var notesRepo = Substitute.For<IReleaseNotesRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        var tenant = Substitute.For<ICurrentTenant>();

        releaseRepo.GetByIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(release);
        notesRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(existingNotes);

        var handler = CreateHandler(releaseRepo, notesRepo, clock, tenant);
        var command = new GenerateReleaseNotesFeature.Command(release.Id.Value, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AlreadyExist");
    }

    [Fact]
    public void Validator_ShouldFail_WhenReleaseIdIsEmpty()
    {
        var validator = new GenerateReleaseNotesFeature.Validator();
        var command = new GenerateReleaseNotesFeature.Command(Guid.Empty, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReleaseId");
    }

    [Fact]
    public void Validator_ShouldPass_WhenAllFieldsValid()
    {
        var validator = new GenerateReleaseNotesFeature.Validator();
        var command = new GenerateReleaseNotesFeature.Command(Guid.NewGuid(), "executive");

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_ShouldPass_WhenPersonaModeIsNull()
    {
        var validator = new GenerateReleaseNotesFeature.Validator();
        var command = new GenerateReleaseNotesFeature.Command(Guid.NewGuid(), null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_ShouldFail_WhenPersonaModeTooLong()
    {
        var validator = new GenerateReleaseNotesFeature.Validator();
        var command = new GenerateReleaseNotesFeature.Command(Guid.NewGuid(), new string('x', 201));

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PersonaMode");
    }
}
