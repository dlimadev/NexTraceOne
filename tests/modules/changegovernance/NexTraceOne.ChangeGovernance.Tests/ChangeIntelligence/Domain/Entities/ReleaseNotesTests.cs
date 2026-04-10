using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Tests.ChangeIntelligence.Domain.Entities;

/// <summary>Testes da entidade ReleaseNotes.</summary>
public sealed class ReleaseNotesTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private static ReleaseNotes CreateValidReleaseNotes() =>
        ReleaseNotes.Create(
            ReleaseNotesId.New(),
            ReleaseId.New(),
            "## Technical Summary",
            "Executive summary text",
            "New endpoints section",
            "Breaking changes section",
            "Affected services section",
            "Confidence metrics section",
            "Evidence links section",
            "template-v1",
            tokensUsed: 150,
            ReleaseNotesStatus.Draft,
            Guid.NewGuid(),
            FixedNow);

    [Fact]
    public void Create_ShouldReturnValidEntity_WhenAllFieldsProvided()
    {
        var releaseId = ReleaseId.New();
        var tenantId = Guid.NewGuid();

        var notes = ReleaseNotes.Create(
            ReleaseNotesId.New(),
            releaseId,
            "## Summary",
            "Executive text",
            "New endpoints",
            "Breaking changes",
            "Affected services",
            "Confidence metrics",
            "Evidence links",
            "template-v1",
            tokensUsed: 100,
            ReleaseNotesStatus.Draft,
            tenantId,
            FixedNow);

        notes.Should().NotBeNull();
        notes.ReleaseId.Should().Be(releaseId);
        notes.TechnicalSummary.Should().Be("## Summary");
        notes.ExecutiveSummary.Should().Be("Executive text");
        notes.NewEndpointsSection.Should().Be("New endpoints");
        notes.BreakingChangesSection.Should().Be("Breaking changes");
        notes.AffectedServicesSection.Should().Be("Affected services");
        notes.ConfidenceMetricsSection.Should().Be("Confidence metrics");
        notes.EvidenceLinksSection.Should().Be("Evidence links");
        notes.ModelUsed.Should().Be("template-v1");
        notes.TokensUsed.Should().Be(100);
        notes.Status.Should().Be(ReleaseNotesStatus.Draft);
        notes.TenantId.Should().Be(tenantId);
        notes.GeneratedAt.Should().Be(FixedNow);
        notes.RegenerationCount.Should().Be(0);
        notes.LastRegeneratedAt.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldThrow_WhenReleaseIdIsNull()
    {
        var act = () => ReleaseNotes.Create(
            ReleaseNotesId.New(),
            null!,
            "Summary",
            null, null, null, null, null, null,
            "template-v1",
            tokensUsed: 0,
            ReleaseNotesStatus.Draft,
            null,
            FixedNow);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_ShouldThrow_WhenTechnicalSummaryIsEmpty()
    {
        var act = () => ReleaseNotes.Create(
            ReleaseNotesId.New(),
            ReleaseId.New(),
            "",
            null, null, null, null, null, null,
            "template-v1",
            tokensUsed: 0,
            ReleaseNotesStatus.Draft,
            null,
            FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldThrow_WhenModelUsedIsEmpty()
    {
        var act = () => ReleaseNotes.Create(
            ReleaseNotesId.New(),
            ReleaseId.New(),
            "Summary",
            null, null, null, null, null, null,
            "",
            tokensUsed: 0,
            ReleaseNotesStatus.Draft,
            null,
            FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldThrow_WhenTokensUsedIsNegative()
    {
        var act = () => ReleaseNotes.Create(
            ReleaseNotesId.New(),
            ReleaseId.New(),
            "Summary",
            null, null, null, null, null, null,
            "template-v1",
            tokensUsed: -1,
            ReleaseNotesStatus.Draft,
            null,
            FixedNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Publish_ShouldChangeStatusToPublished()
    {
        var notes = CreateValidReleaseNotes();

        notes.Publish();

        notes.Status.Should().Be(ReleaseNotesStatus.Published);
    }

    [Fact]
    public void Archive_ShouldChangeStatusToArchived()
    {
        var notes = CreateValidReleaseNotes();

        notes.Archive();

        notes.Status.Should().Be(ReleaseNotesStatus.Archived);
    }

    [Fact]
    public void Regenerate_ShouldUpdateContent_AndIncrementCount()
    {
        var notes = CreateValidReleaseNotes();
        var regeneratedAt = FixedNow.AddHours(1);

        notes.Regenerate(
            "## Updated Summary",
            "Updated executive",
            "Updated endpoints",
            "Updated breaking",
            "Updated affected",
            "Updated confidence",
            "Updated evidence",
            "template-v2",
            tokensUsed: 200,
            regeneratedAt);

        notes.TechnicalSummary.Should().Be("## Updated Summary");
        notes.ExecutiveSummary.Should().Be("Updated executive");
        notes.NewEndpointsSection.Should().Be("Updated endpoints");
        notes.BreakingChangesSection.Should().Be("Updated breaking");
        notes.AffectedServicesSection.Should().Be("Updated affected");
        notes.ConfidenceMetricsSection.Should().Be("Updated confidence");
        notes.EvidenceLinksSection.Should().Be("Updated evidence");
        notes.ModelUsed.Should().Be("template-v2");
        notes.TokensUsed.Should().Be(200);
        notes.Status.Should().Be(ReleaseNotesStatus.Draft);
        notes.LastRegeneratedAt.Should().Be(regeneratedAt);
        notes.RegenerationCount.Should().Be(1);
    }

    [Fact]
    public void Regenerate_ShouldIncrementCountMultipleTimes()
    {
        var notes = CreateValidReleaseNotes();

        notes.Regenerate("V2", null, null, null, null, null, null, "template-v1", 0, FixedNow.AddHours(1));
        notes.Regenerate("V3", null, null, null, null, null, null, "template-v1", 0, FixedNow.AddHours(2));
        notes.Regenerate("V4", null, null, null, null, null, null, "template-v1", 0, FixedNow.AddHours(3));

        notes.RegenerationCount.Should().Be(3);
        notes.TechnicalSummary.Should().Be("V4");
    }

    [Fact]
    public void Regenerate_ShouldResetStatusToDraft()
    {
        var notes = CreateValidReleaseNotes();
        notes.Publish();
        notes.Status.Should().Be(ReleaseNotesStatus.Published);

        notes.Regenerate("Regenerated", null, null, null, null, null, null, "template-v1", 0, FixedNow.AddHours(1));

        notes.Status.Should().Be(ReleaseNotesStatus.Draft);
    }

    [Fact]
    public void ReleaseNotesId_New_ShouldCreateUniqueIds()
    {
        var id1 = ReleaseNotesId.New();
        var id2 = ReleaseNotesId.New();

        id1.Should().NotBe(id2);
    }

    [Fact]
    public void ReleaseNotesId_From_ShouldPreserveGuid()
    {
        var guid = Guid.NewGuid();
        var id = ReleaseNotesId.From(guid);

        id.Value.Should().Be(guid);
    }
}
