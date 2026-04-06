using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Knowledge.Application.Abstractions;
using NexTraceOne.Knowledge.Application.Features.GenerateAutoDocumentation;
using NexTraceOne.Knowledge.Application.Features.GetKnowledgeGraphOverview;
using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Tests.Application.Features;

/// <summary>
/// Testes unitários para GenerateAutoDocumentation e GetKnowledgeGraphOverview.
/// </summary>
public sealed class KnowledgeIntelligenceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 6, 12, 0, 0, TimeSpan.Zero);

    private readonly IKnowledgeDocumentRepository _documentRepo = Substitute.For<IKnowledgeDocumentRepository>();
    private readonly IKnowledgeRelationRepository _relationRepo = Substitute.For<IKnowledgeRelationRepository>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public KnowledgeIntelligenceTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    // ── GenerateAutoDocumentation ─────────────────────────────────────────────

    [Fact]
    public async Task GenerateAutoDocumentation_ValidService_ReturnsAllSections()
    {
        _documentRepo.ListAsync(DocumentCategory.Runbook, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<KnowledgeDocument>().AsReadOnly() as IReadOnlyList<KnowledgeDocument>, 0));

        var handler = new GenerateAutoDocumentation.Handler(_documentRepo, _clock);
        var query = new GenerateAutoDocumentation.Query("MyService");

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceName.Should().Be("MyService");
        result.Value.TotalSections.Should().Be(7);
        result.Value.GeneratedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task GenerateAutoDocumentation_SpecificSections_ReturnsOnlyRequested()
    {
        _documentRepo.ListAsync(DocumentCategory.Runbook, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<KnowledgeDocument>().AsReadOnly() as IReadOnlyList<KnowledgeDocument>, 0));

        var handler = new GenerateAutoDocumentation.Handler(_documentRepo, _clock);
        var query = new GenerateAutoDocumentation.Query("MyService", ["Overview", "Runbooks"]);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalSections.Should().Be(2);
        result.Value.Sections.Should().Contain(s => s.Title == "Overview");
        result.Value.Sections.Should().Contain(s => s.Title == "Runbooks");
    }

    [Fact]
    public void GenerateAutoDocumentation_EmptyServiceName_FailsValidation()
    {
        var validator = new GenerateAutoDocumentation.Validator();
        var result = validator.Validate(new GenerateAutoDocumentation.Query(""));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateAutoDocumentation_WithRunbooks_ShowsCount()
    {
        var runbook = KnowledgeDocument.Create(
            "My Runbook", "Content", null, DocumentCategory.Runbook, null, Guid.NewGuid(), FixedNow.AddDays(-1));

        _documentRepo.ListAsync(DocumentCategory.Runbook, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<KnowledgeDocument>)[runbook], 1));

        var handler = new GenerateAutoDocumentation.Handler(_documentRepo, _clock);
        var query = new GenerateAutoDocumentation.Query("MyService", ["Runbooks"]);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Sections[0].Content.Should().Contain("1 runbook(s)");
    }

    // ── GetKnowledgeGraphOverview ─────────────────────────────────────────────

    [Fact]
    public async Task GetKnowledgeGraphOverview_NoDocuments_ReturnsEmptyGraph()
    {
        _documentRepo.ListAsync(null, null, 1, 500, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<KnowledgeDocument>)[], 0));

        var handler = new GetKnowledgeGraphOverview.Handler(_documentRepo, _relationRepo, _clock);
        var query = new GetKnowledgeGraphOverview.Query(null, null, 2);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalNodes.Should().Be(0);
        result.Value.TotalEdges.Should().Be(0);
        result.Value.ConnectedComponents.Should().Be(0);
    }

    [Fact]
    public async Task GetKnowledgeGraphOverview_WithDocumentsNoRelations_ReturnsIsolatedNodes()
    {
        var doc = KnowledgeDocument.Create("Doc1", "Content", null, DocumentCategory.General, null, Guid.NewGuid(), FixedNow);
        _documentRepo.ListAsync(null, null, 1, 500, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<KnowledgeDocument>)[doc], 1));
        _relationRepo.ListBySourceAsync(doc.Id.Value, Arg.Any<CancellationToken>())
            .Returns(new List<KnowledgeRelation>().AsReadOnly() as IReadOnlyList<KnowledgeRelation>);

        var handler = new GetKnowledgeGraphOverview.Handler(_documentRepo, _relationRepo, _clock);
        var query = new GetKnowledgeGraphOverview.Query(null, null, 2);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalNodes.Should().Be(1);
        result.Value.TotalEdges.Should().Be(0);
        result.Value.ConnectedComponents.Should().Be(1);
    }

    [Fact]
    public void GetKnowledgeGraphOverview_InvalidMaxDepth_FailsValidation()
    {
        var validator = new GetKnowledgeGraphOverview.Validator();
        var result = validator.Validate(new GetKnowledgeGraphOverview.Query(null, null, 10));
        result.IsValid.Should().BeFalse();
    }
}
