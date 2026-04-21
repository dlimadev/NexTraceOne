using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Application.Features.GetFreshnessReport;
using NexTraceOne.Knowledge.Application.Features.ProposeRunbookFromIncident;
using NexTraceOne.Knowledge.Application.Features.ScoreDocumentFreshness;
using NexTraceOne.Knowledge.Application.Features.SearchAcrossModules;
using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Tests.Application.Features;

/// <summary>
/// Testes unitários — WAVE B.4: Knowledge Hub.
/// Cobre FreshnessScore, ProposedRunbook domain, ScoreDocumentFreshness, GetFreshnessReport,
/// ProposeRunbookFromIncident e SearchAcrossModules.
/// </summary>
public sealed class KnowledgeHubB4Tests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 20, 12, 0, 0, TimeSpan.Zero);

    // ── KnowledgeDocument.ComputeFreshnessScore ───────────────────────────

    [Fact]
    public void KnowledgeDocument_ComputeFreshnessScore_RecentDoc_IsAbove80()
    {
        var doc = CreateDoc(createdAt: FixedNow.AddDays(-10));

        doc.ComputeFreshnessScore(FixedNow);

        doc.FreshnessScore.Should().BeGreaterThan(80);
    }

    [Fact]
    public void KnowledgeDocument_ComputeFreshnessScore_OldDoc_IsZero()
    {
        var doc = CreateDoc(createdAt: FixedNow.AddDays(-200));

        doc.ComputeFreshnessScore(FixedNow);

        doc.FreshnessScore.Should().Be(0);
    }

    [Fact]
    public void KnowledgeDocument_ComputeFreshnessScore_ExactlyAt180Days_IsZero()
    {
        var doc = CreateDoc(createdAt: FixedNow.AddDays(-180));

        doc.ComputeFreshnessScore(FixedNow);

        doc.FreshnessScore.Should().Be(0);
    }

    // ── KnowledgeDocument.MarkReviewed ────────────────────────────────────

    [Fact]
    public void KnowledgeDocument_MarkReviewed_SetsFreshnessTo100AndReviewedBy()
    {
        var doc = CreateDoc(createdAt: FixedNow.AddDays(-100));
        doc.ComputeFreshnessScore(FixedNow); // sets low score

        doc.MarkReviewed("jane.doe", FixedNow);

        doc.FreshnessScore.Should().Be(100);
        doc.ReviewedBy.Should().Be("jane.doe");
        doc.LastReviewedAt.Should().Be(FixedNow);
    }

    // ── ProposedRunbook.Create ────────────────────────────────────────────

    [Fact]
    public void ProposedRunbook_Create_WithValidArgs_StatusIsProposed()
    {
        var incidentId = Guid.NewGuid();
        var runbook = ProposedRunbook.Create(
            title: "Runbook: DB timeout",
            contentMarkdown: "## Steps\n1. Restart service",
            sourceIncidentId: incidentId,
            proposedAt: FixedNow);

        runbook.Title.Should().Be("Runbook: DB timeout");
        runbook.Status.Should().Be(ProposedRunbookStatus.Proposed);
        runbook.SourceIncidentId.Should().Be(incidentId);
    }

    // ── ProposedRunbook.Approve ───────────────────────────────────────────

    [Fact]
    public void ProposedRunbook_Approve_SetsStatusAndReviewer()
    {
        var runbook = ProposedRunbook.Create("title", "content", Guid.NewGuid(), FixedNow);

        runbook.Approve("tech.lead", FixedNow.AddHours(2), "Looks good");

        runbook.Status.Should().Be(ProposedRunbookStatus.Approved);
        runbook.ReviewedBy.Should().Be("tech.lead");
        runbook.ReviewNote.Should().Be("Looks good");
    }

    // ── ProposedRunbook.Reject ────────────────────────────────────────────

    [Fact]
    public void ProposedRunbook_Reject_SetsStatusAndNote()
    {
        var runbook = ProposedRunbook.Create("title", "content", Guid.NewGuid(), FixedNow);

        runbook.Reject("tech.lead", FixedNow.AddHours(1), "Incomplete resolution");

        runbook.Status.Should().Be(ProposedRunbookStatus.Rejected);
        runbook.ReviewNote.Should().Be("Incomplete resolution");
    }

    // ── ScoreDocumentFreshness ────────────────────────────────────────────

    [Fact]
    public async Task ScoreDocumentFreshness_ValidDoc_UpdatesFreshnessScore()
    {
        var repo = Substitute.For<IKnowledgeDocumentRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var docId = Guid.NewGuid();
        var doc = CreateDoc(createdAt: FixedNow.AddDays(-90), id: docId);
        repo.GetByIdAsync(Arg.Any<KnowledgeDocumentId>(), Arg.Any<CancellationToken>()).Returns(doc);

        var handler = new ScoreDocumentFreshness.Handler(repo, clock);
        var result = await handler.Handle(new ScoreDocumentFreshness.Command(docId.ToString()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FreshnessScore.Should().BeGreaterThan(0).And.BeLessThan(101);
        repo.Received(1).Update(doc);
    }

    [Fact]
    public async Task ScoreDocumentFreshness_InvalidGuid_ReturnsValidationError()
    {
        var repo = Substitute.For<IKnowledgeDocumentRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var handler = new ScoreDocumentFreshness.Handler(repo, clock);
        var result = await handler.Handle(new ScoreDocumentFreshness.Command("not-a-guid"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    // ── GetFreshnessReport ────────────────────────────────────────────────

    [Fact]
    public async Task GetFreshnessReport_ReturnsStaleAgingFreshCounts()
    {
        var repo = Substitute.For<IKnowledgeDocumentRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var docs = new List<KnowledgeDocument>
        {
            CreateDoc(createdAt: FixedNow.AddDays(-10)),   // Fresh (score ~94)
            CreateDoc(createdAt: FixedNow.AddDays(-70)),   // Aging (score ~61)
            CreateDoc(createdAt: FixedNow.AddDays(-200)),  // Stale (score 0)
        };
        repo.ListAsync(null, null, 1, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((docs, 3));

        var handler = new GetFreshnessReport.Handler(repo, clock);
        var result = await handler.Handle(new GetFreshnessReport.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalDocuments.Should().Be(3);
        result.Value.FreshCount.Should().Be(1);
        result.Value.AgingCount.Should().Be(1);
        result.Value.StaleCount.Should().Be(1);
    }

    // ── ProposeRunbookFromIncident ────────────────────────────────────────

    [Fact]
    public async Task ProposeRunbookFromIncident_NewIncident_CreatesRunbook()
    {
        var repo = Substitute.For<IProposedRunbookRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        repo.GetByIncidentIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ProposedRunbook?)null);

        var handler = new ProposeRunbookFromIncident.Handler(repo, clock);
        var command = new ProposeRunbookFromIncident.Command(
            IncidentId: Guid.NewGuid(),
            IncidentTitle: "DB connection timeout",
            ResolutionSummary: "Restarted connection pool.");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AlreadyExisted.Should().BeFalse();
        result.Value.Status.Should().Be("Proposed");
        await repo.Received(1).AddAsync(Arg.Any<ProposedRunbook>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProposeRunbookFromIncident_ExistingIncident_ReturnsExisting()
    {
        var repo = Substitute.For<IProposedRunbookRepository>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(FixedNow);

        var incidentId = Guid.NewGuid();
        var existingRunbook = ProposedRunbook.Create("Runbook: DB timeout", "content", incidentId, FixedNow);
        repo.GetByIncidentIdAsync(incidentId, Arg.Any<CancellationToken>()).Returns(existingRunbook);

        var handler = new ProposeRunbookFromIncident.Handler(repo, clock);
        var result = await handler.Handle(new ProposeRunbookFromIncident.Command(incidentId, "DB timeout", "resolution"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AlreadyExisted.Should().BeTrue();
        await repo.DidNotReceive().AddAsync(Arg.Any<ProposedRunbook>(), Arg.Any<CancellationToken>());
    }

    // ── SearchAcrossModules ───────────────────────────────────────────────

    [Fact]
    public async Task SearchAcrossModules_ReturnsCombinedResults()
    {
        var docRepo = Substitute.For<IKnowledgeDocumentRepository>();
        var noteRepo = Substitute.For<IOperationalNoteRepository>();
        var runbookRepo = Substitute.For<IProposedRunbookRepository>();

        docRepo.SearchAsync("timeout", Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<KnowledgeDocument> { CreateDoc(createdAt: FixedNow, title: "DB timeout guide") });

        noteRepo.SearchAsync("timeout", Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<NexTraceOne.Knowledge.Domain.Entities.OperationalNote>());

        runbookRepo.ListAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(new List<ProposedRunbook>
            {
                ProposedRunbook.Create("Runbook: timeout fix", "# timeout\nSteps...", Guid.NewGuid(), FixedNow)
            });

        var handler = new SearchAcrossModules.Handler(docRepo, noteRepo, runbookRepo);
        var result = await handler.Handle(new SearchAcrossModules.Query("timeout"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalResults.Should().BeGreaterThan(0);
        result.Value.Results.Should().Contain(r => r.Type == "Document");
        result.Value.Results.Should().Contain(r => r.Type == "ProposedRunbook");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static KnowledgeDocument CreateDoc(DateTimeOffset createdAt, Guid? id = null, string title = "Test Document")
    {
        var doc = KnowledgeDocument.Create(
            title: title,
            content: "Document content",
            summary: null,
            category: DocumentCategory.General,
            tags: new List<string>(),
            authorId: Guid.NewGuid(),
            utcNow: createdAt);
        return doc;
    }
}
