using System.Linq;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Tools;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Tools;

/// <summary>
/// Testes unitários para as três novas tools adicionadas em P1 (fase 2):
/// SearchKnowledgeTool, GetRunbookTool, ListContractVersionsTool.
/// </summary>
public sealed class AdditionalPhantomToolsTests
{
    // ── SearchKnowledgeTool ───────────────────────────────────────────────

    [Fact]
    public async Task SearchKnowledgeTool_ShouldHaveCorrectDefinition()
    {
        var knowledgeReader = Substitute.For<IKnowledgeDocumentGroundingReader>();
        knowledgeReader.SearchDocumentsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<KnowledgeDocumentGroundingContext>() as IReadOnlyList<KnowledgeDocumentGroundingContext>);

        var tool = new SearchKnowledgeTool(knowledgeReader, Substitute.For<ILogger<SearchKnowledgeTool>>());

        tool.Definition.Name.Should().Be("search_knowledge");
        tool.Definition.Category.Should().Be("knowledge");
        tool.Definition.Parameters.Should().Contain(p => p.Name == "query" && p.Required == true);
    }

    [Fact]
    public async Task SearchKnowledgeTool_ShouldFailWhenQueryMissing()
    {
        var knowledgeReader = Substitute.For<IKnowledgeDocumentGroundingReader>();
        var tool = new SearchKnowledgeTool(knowledgeReader, Substitute.For<ILogger<SearchKnowledgeTool>>());

        var result = await tool.ExecuteAsync("{}", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("query");
    }

    [Fact]
    public async Task SearchKnowledgeTool_ShouldReturnDocumentsForValidQuery()
    {
        var knowledgeReader = Substitute.For<IKnowledgeDocumentGroundingReader>();
        knowledgeReader.SearchDocumentsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<KnowledgeDocumentGroundingContext>
            {
                new("doc-001", "Payment Service Runbook", "How to handle payment failures", "Runbook"),
                new("doc-002", "API Contract Guide", "REST contract governance", "Guide"),
            } as IReadOnlyList<KnowledgeDocumentGroundingContext>);

        var tool = new SearchKnowledgeTool(knowledgeReader, Substitute.For<ILogger<SearchKnowledgeTool>>());

        var result = await tool.ExecuteAsync("{\"query\":\"payment\"}", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Output.Should().Contain("search_knowledge");
        result.Output.Should().Contain("doc-001");
    }

    [Fact]
    public async Task SearchKnowledgeTool_ShouldFilterByCategory()
    {
        var knowledgeReader = Substitute.For<IKnowledgeDocumentGroundingReader>();
        knowledgeReader.SearchDocumentsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<KnowledgeDocumentGroundingContext>
            {
                new("doc-001", "Payment Runbook", "Runbook content", "Runbook"),
                new("doc-002", "API Guide", "Guide content", "Guide"),
            } as IReadOnlyList<KnowledgeDocumentGroundingContext>);

        var tool = new SearchKnowledgeTool(knowledgeReader, Substitute.For<ILogger<SearchKnowledgeTool>>());

        var result = await tool.ExecuteAsync("{\"query\":\"payment\",\"category\":\"Runbook\"}", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Output.Should().Contain("doc-001");
        result.Output.Should().NotContain("doc-002");
    }

    [Fact]
    public async Task SearchKnowledgeTool_ShouldHandleEmptyResults()
    {
        var knowledgeReader = Substitute.For<IKnowledgeDocumentGroundingReader>();
        knowledgeReader.SearchDocumentsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<KnowledgeDocumentGroundingContext>() as IReadOnlyList<KnowledgeDocumentGroundingContext>);

        var tool = new SearchKnowledgeTool(knowledgeReader, Substitute.For<ILogger<SearchKnowledgeTool>>());

        var result = await tool.ExecuteAsync("{\"query\":\"nonexistent topic\"}", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Output.Should().Contain("\"total\":0");
    }

    // ── GetRunbookTool ────────────────────────────────────────────────────

    [Fact]
    public async Task GetRunbookTool_ShouldHaveCorrectDefinition()
    {
        var knowledgeReader = Substitute.For<IKnowledgeDocumentGroundingReader>();
        var tool = new GetRunbookTool(knowledgeReader, Substitute.For<ILogger<GetRunbookTool>>());

        tool.Definition.Name.Should().Be("get_runbook");
        tool.Definition.Category.Should().Be("knowledge");
        tool.Definition.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetRunbookTool_ShouldReturnRunbooksForService()
    {
        var knowledgeReader = Substitute.For<IKnowledgeDocumentGroundingReader>();
        knowledgeReader.SearchDocumentsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<KnowledgeDocumentGroundingContext>
            {
                new("rb-001", "Payment Failover Runbook", "Steps to handle payment service failover", "Runbook"),
                new("rb-002", "Database Restart Runbook", "Steps to restart the DB safely", "Runbook"),
            } as IReadOnlyList<KnowledgeDocumentGroundingContext>);

        var tool = new GetRunbookTool(knowledgeReader, Substitute.For<ILogger<GetRunbookTool>>());

        var result = await tool.ExecuteAsync("{\"serviceName\":\"payment-service\"}", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Output.Should().Contain("get_runbook");
        result.Output.Should().Contain("rb-001");
    }

    [Fact]
    public async Task GetRunbookTool_ShouldIncludeGuidanceWhenNoRunbooks()
    {
        var knowledgeReader = Substitute.For<IKnowledgeDocumentGroundingReader>();
        knowledgeReader.SearchDocumentsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<KnowledgeDocumentGroundingContext>() as IReadOnlyList<KnowledgeDocumentGroundingContext>);

        var tool = new GetRunbookTool(knowledgeReader, Substitute.For<ILogger<GetRunbookTool>>());

        var result = await tool.ExecuteAsync("{\"serviceName\":\"orphan-service\"}", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Output.Should().Contain("No runbooks found");
    }

    [Fact]
    public async Task GetRunbookTool_ShouldWorkWithKeywordsOnly()
    {
        var knowledgeReader = Substitute.For<IKnowledgeDocumentGroundingReader>();
        knowledgeReader.SearchDocumentsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<KnowledgeDocumentGroundingContext>() as IReadOnlyList<KnowledgeDocumentGroundingContext>);

        var tool = new GetRunbookTool(knowledgeReader, Substitute.For<ILogger<GetRunbookTool>>());

        var result = await tool.ExecuteAsync("{\"keywords\":\"database rollback\"}", CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    // ── ListContractVersionsTool ──────────────────────────────────────────

    [Fact]
    public async Task ListContractVersionsTool_ShouldHaveCorrectDefinition()
    {
        var changeReader = Substitute.For<IChangeGroundingReader>();
        var tool = new ListContractVersionsTool(changeReader, Substitute.For<ILogger<ListContractVersionsTool>>());

        tool.Definition.Name.Should().Be("list_contract_versions");
        tool.Definition.Category.Should().Be("contract_governance");
        tool.Definition.Parameters.Should().Contain(p => p.Name == "serviceId" && p.Required == true);
    }

    [Fact]
    public async Task ListContractVersionsTool_ShouldFailWhenServiceIdMissing()
    {
        var changeReader = Substitute.For<IChangeGroundingReader>();
        var tool = new ListContractVersionsTool(changeReader, Substitute.For<ILogger<ListContractVersionsTool>>());

        var result = await tool.ExecuteAsync("{}", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("serviceId");
    }

    [Fact]
    public async Task ListContractVersionsTool_ShouldReturnVersionsForValidService()
    {
        var changeReader = Substitute.For<IChangeGroundingReader>();
        changeReader.FindRecentReleasesAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<Guid?>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ReleaseGroundingContext>
            {
                new("rel-001", "payment-api", "2.1.0", "production", "Deployed", "Minor", 0.3m, "Feature release", DateTimeOffset.UtcNow.AddDays(-5)),
                new("rel-002", "payment-api", "2.0.1", "staging", "Deployed", "Patch", 0.1m, "Bug fix", DateTimeOffset.UtcNow.AddDays(-10)),
            } as IReadOnlyList<ReleaseGroundingContext>);

        var tool = new ListContractVersionsTool(changeReader, Substitute.For<ILogger<ListContractVersionsTool>>());

        var result = await tool.ExecuteAsync("{\"serviceId\":\"payment-api\"}", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Output.Should().Contain("list_contract_versions");
        result.Output.Should().Contain("2.1.0");
        result.Output.Should().Contain("rel-001");
    }

    [Fact]
    public async Task ListContractVersionsTool_ShouldIncludeGuidanceWhenNoVersions()
    {
        var changeReader = Substitute.For<IChangeGroundingReader>();
        changeReader.FindRecentReleasesAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<Guid?>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ReleaseGroundingContext>() as IReadOnlyList<ReleaseGroundingContext>);

        var tool = new ListContractVersionsTool(changeReader, Substitute.For<ILogger<ListContractVersionsTool>>());

        var result = await tool.ExecuteAsync("{\"serviceId\":\"new-service\"}", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Output.Should().Contain("No releases found");
    }

    // ── All 9 tools have unique names ─────────────────────────────────────

    [Fact]
    public void AllNineTools_ShouldHaveUniqueNames()
    {
        var catalogReader = Substitute.For<ICatalogGroundingReader>();
        var incidentReader = Substitute.For<IIncidentGroundingReader>();
        var knowledgeReader = Substitute.For<IKnowledgeDocumentGroundingReader>();
        var changeReader = Substitute.For<IChangeGroundingReader>();
        var ledger = Substitute.For<NexTraceOne.AIKnowledge.Application.Governance.Abstractions.IAiTokenUsageLedgerRepository>();

        var tools = new IAgentTool[]
        {
            new ListServicesInfoTool(Substitute.For<ILogger<ListServicesInfoTool>>()),
            new GetServiceHealthTool(Substitute.For<ILogger<GetServiceHealthTool>>()),
            new ListRecentChangesTool(Substitute.For<ILogger<ListRecentChangesTool>>()),
            new GetContractDetailsTool(catalogReader, Substitute.For<ILogger<GetContractDetailsTool>>()),
            new SearchIncidentsTool(incidentReader, Substitute.For<ILogger<SearchIncidentsTool>>()),
            new GetTokenUsageSummaryTool(ledger, Substitute.For<ILogger<GetTokenUsageSummaryTool>>()),
            new SearchKnowledgeTool(knowledgeReader, Substitute.For<ILogger<SearchKnowledgeTool>>()),
            new GetRunbookTool(knowledgeReader, Substitute.For<ILogger<GetRunbookTool>>()),
            new ListContractVersionsTool(changeReader, Substitute.For<ILogger<ListContractVersionsTool>>()),
        };

        tools.Select(t => t.Definition.Name).Should().OnlyHaveUniqueItems();
        tools.Should().HaveCount(9);
    }
}
