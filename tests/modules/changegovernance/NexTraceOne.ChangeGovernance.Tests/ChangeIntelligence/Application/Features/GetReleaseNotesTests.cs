using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

using GetReleaseNotesFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetReleaseNotes.GetReleaseNotes;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Application.Features;

/// <summary>Testes do handler GetReleaseNotes.</summary>
public sealed class GetReleaseNotesTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_ShouldReturnReleaseNotes_WhenFound()
    {
        var releaseId = ReleaseId.New();
        var notes = ReleaseNotes.Create(
            ReleaseNotesId.New(),
            releaseId,
            "## Technical Summary",
            "Executive summary",
            "New endpoints",
            "Breaking changes",
            "Affected services",
            "Confidence metrics",
            "Evidence links",
            "template-v1",
            tokensUsed: 100,
            ReleaseNotesStatus.Draft,
            Guid.NewGuid(),
            FixedNow);

        var notesRepo = Substitute.For<IReleaseNotesRepository>();
        notesRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns(notes);

        var handler = new GetReleaseNotesFeature.Handler(notesRepo);
        var query = new GetReleaseNotesFeature.Query(releaseId.Value);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReleaseId.Should().Be(releaseId.Value);
        result.Value.TechnicalSummary.Should().Be("## Technical Summary");
        result.Value.ExecutiveSummary.Should().Be("Executive summary");
        result.Value.NewEndpointsSection.Should().Be("New endpoints");
        result.Value.BreakingChangesSection.Should().Be("Breaking changes");
        result.Value.AffectedServicesSection.Should().Be("Affected services");
        result.Value.ConfidenceMetricsSection.Should().Be("Confidence metrics");
        result.Value.EvidenceLinksSection.Should().Be("Evidence links");
        result.Value.ModelUsed.Should().Be("template-v1");
        result.Value.TokensUsed.Should().Be(100);
        result.Value.Status.Should().Be("Draft");
        result.Value.GeneratedAt.Should().Be(FixedNow);
        result.Value.RegenerationCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenNotFound()
    {
        var notesRepo = Substitute.For<IReleaseNotesRepository>();
        notesRepo.GetByReleaseIdAsync(Arg.Any<ReleaseId>(), Arg.Any<CancellationToken>())
            .Returns((ReleaseNotes?)null);

        var handler = new GetReleaseNotesFeature.Handler(notesRepo);
        var query = new GetReleaseNotesFeature.Query(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public void Validator_ShouldFail_WhenReleaseIdIsEmpty()
    {
        var validator = new GetReleaseNotesFeature.Validator();
        var query = new GetReleaseNotesFeature.Query(Guid.Empty);

        var result = validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReleaseId");
    }

    [Fact]
    public void Validator_ShouldPass_WhenReleaseIdIsValid()
    {
        var validator = new GetReleaseNotesFeature.Validator();
        var query = new GetReleaseNotesFeature.Query(Guid.NewGuid());

        var result = validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }
}
