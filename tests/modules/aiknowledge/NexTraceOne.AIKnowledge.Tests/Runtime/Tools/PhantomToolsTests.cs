using System.Linq;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Tools;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Tools;

/// <summary>
/// Testes unitários para as três novas tools: GetContractDetailsTool, SearchIncidentsTool, GetTokenUsageSummaryTool.
/// </summary>
public sealed class PhantomToolsTests
{
    // ── GetContractDetailsTool ────────────────────────────────────────────

    [Fact]
    public async Task GetContractDetailsTool_ShouldHaveCorrectDefinition()
    {
        var catalogReader = Substitute.For<ICatalogGroundingReader>();
        catalogReader.FindServicesAsync(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ServiceGroundingContext>() as IReadOnlyList<ServiceGroundingContext>);

        var tool = new GetContractDetailsTool(catalogReader, Substitute.For<ILogger<GetContractDetailsTool>>());

        tool.Definition.Name.Should().Be("get_contract_details");
        tool.Definition.Category.Should().Be("contract_governance");
        tool.Definition.Description.Should().NotBeNullOrWhiteSpace();
        tool.Definition.Parameters.Should().Contain(p => p.Name == "contractId" && p.Required == true);
    }

    [Fact]
    public async Task GetContractDetailsTool_ShouldFailWhenContractIdMissing()
    {
        var catalogReader = Substitute.For<ICatalogGroundingReader>();
        var tool = new GetContractDetailsTool(catalogReader, Substitute.For<ILogger<GetContractDetailsTool>>());

        var result = await tool.ExecuteAsync("{}", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("contractId");
    }

    [Fact]
    public async Task GetContractDetailsTool_ShouldSucceedWithValidArgs()
    {
        var catalogReader = Substitute.For<ICatalogGroundingReader>();
        catalogReader.FindServicesAsync(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ServiceGroundingContext>
            {
                new("svc-001", "Payment API", "Payments Team", "Finance", "Critical", "Active", "REST", "Handles payments"),
            } as IReadOnlyList<ServiceGroundingContext>);

        var tool = new GetContractDetailsTool(catalogReader, Substitute.For<ILogger<GetContractDetailsTool>>());

        var result = await tool.ExecuteAsync("{\"contractId\":\"payment-api-v2\"}", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ToolName.Should().Be("get_contract_details");
        result.Output.Should().Contain("get_contract_details");
        result.Output.Should().Contain("Payment API");
    }

    [Fact]
    public async Task GetContractDetailsTool_ShouldHandleInvalidJson()
    {
        var catalogReader = Substitute.For<ICatalogGroundingReader>();
        var tool = new GetContractDetailsTool(catalogReader, Substitute.For<ILogger<GetContractDetailsTool>>());

        var result = await tool.ExecuteAsync("not-json", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("contractId");
    }

    // ── SearchIncidentsTool ───────────────────────────────────────────────

    [Fact]
    public async Task SearchIncidentsTool_ShouldHaveCorrectDefinition()
    {
        var incidentReader = Substitute.For<IIncidentGroundingReader>();
        var tool = new SearchIncidentsTool(incidentReader, Substitute.For<ILogger<SearchIncidentsTool>>());

        tool.Definition.Name.Should().Be("search_incidents");
        tool.Definition.Category.Should().Be("operations");
        tool.Definition.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SearchIncidentsTool_ShouldReturnIncidents()
    {
        var now = DateTimeOffset.UtcNow;
        var incidentReader = Substitute.For<IIncidentGroundingReader>();
        incidentReader.FindRecentIncidentsAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<IncidentGroundingContext>
            {
                new("INC-001", "Payment service timeout", "payment-api", "High", "Open", "production", null, now.AddHours(-2)),
                new("INC-002", "Auth service degraded", "auth-api", "Medium", "Resolved", "production", null, now.AddHours(-5)),
            } as IReadOnlyList<IncidentGroundingContext>);

        var tool = new SearchIncidentsTool(incidentReader, Substitute.For<ILogger<SearchIncidentsTool>>());
        var result = await tool.ExecuteAsync("{\"days\":7}", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Output.Should().Contain("search_incidents");
        result.Output.Should().Contain("INC-001");
        result.Output.Should().Contain("INC-002");
    }

    [Fact]
    public async Task SearchIncidentsTool_ShouldFilterBySeverity()
    {
        var now = DateTimeOffset.UtcNow;
        var incidentReader = Substitute.For<IIncidentGroundingReader>();
        incidentReader.FindRecentIncidentsAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<IncidentGroundingContext>
            {
                new("INC-001", "Timeout", "payment-api", "High", "Open", "production", null, now.AddHours(-1)),
                new("INC-002", "Degraded", "auth-api", "Medium", "Open", "production", null, now.AddHours(-2)),
            } as IReadOnlyList<IncidentGroundingContext>);

        var tool = new SearchIncidentsTool(incidentReader, Substitute.For<ILogger<SearchIncidentsTool>>());
        var result = await tool.ExecuteAsync("{\"severity\":\"High\"}", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Output.Should().Contain("INC-001");
        result.Output.Should().NotContain("INC-002");
    }

    [Fact]
    public async Task SearchIncidentsTool_ShouldHandleEmptyArgs()
    {
        var incidentReader = Substitute.For<IIncidentGroundingReader>();
        incidentReader.FindRecentIncidentsAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<IncidentGroundingContext>() as IReadOnlyList<IncidentGroundingContext>);

        var tool = new SearchIncidentsTool(incidentReader, Substitute.For<ILogger<SearchIncidentsTool>>());
        var result = await tool.ExecuteAsync("", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Output.Should().Contain("search_incidents");
    }

    // ── GetTokenUsageSummaryTool ──────────────────────────────────────────

    [Fact]
    public async Task GetTokenUsageSummaryTool_ShouldHaveCorrectDefinition()
    {
        var ledger = Substitute.For<IAiTokenUsageLedgerRepository>();
        var tool = new GetTokenUsageSummaryTool(ledger, Substitute.For<ILogger<GetTokenUsageSummaryTool>>());

        tool.Definition.Name.Should().Be("get_token_usage_summary");
        tool.Definition.Category.Should().Be("ai_governance");
        tool.Definition.Parameters.Should().Contain(p => p.Name == "scope" && p.Required == true);
        tool.Definition.Parameters.Should().Contain(p => p.Name == "scopeValue" && p.Required == true);
    }

    [Fact]
    public async Task GetTokenUsageSummaryTool_ShouldFailWhenScopeMissing()
    {
        var ledger = Substitute.For<IAiTokenUsageLedgerRepository>();
        var tool = new GetTokenUsageSummaryTool(ledger, Substitute.For<ILogger<GetTokenUsageSummaryTool>>());

        var result = await tool.ExecuteAsync("{}", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("scope");
    }

    [Fact]
    public async Task GetTokenUsageSummaryTool_ShouldFailWhenScopeValueMissing()
    {
        var ledger = Substitute.For<IAiTokenUsageLedgerRepository>();
        var tool = new GetTokenUsageSummaryTool(ledger, Substitute.For<ILogger<GetTokenUsageSummaryTool>>());

        var result = await tool.ExecuteAsync("{\"scope\":\"user\"}", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("scopeValue");
    }

    [Fact]
    public async Task GetTokenUsageSummaryTool_ShouldSucceedForUserScope()
    {
        var ledger = Substitute.For<IAiTokenUsageLedgerRepository>();
        ledger.GetTotalTokensForPeriodAsync(
            Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns(1234L);
        ledger.GetByUserAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<NexTraceOne.AIKnowledge.Domain.Governance.Entities.AiTokenUsageLedger>() as IReadOnlyList<NexTraceOne.AIKnowledge.Domain.Governance.Entities.AiTokenUsageLedger>);

        var tool = new GetTokenUsageSummaryTool(ledger, Substitute.For<ILogger<GetTokenUsageSummaryTool>>());
        var result = await tool.ExecuteAsync("{\"scope\":\"user\",\"scopeValue\":\"user-001\",\"period\":\"week\"}", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Output.Should().Contain("get_token_usage_summary");
        result.Output.Should().Contain("1234");
    }

    [Fact]
    public async Task GetTokenUsageSummaryTool_ShouldFailForInvalidScope()
    {
        var ledger = Substitute.For<IAiTokenUsageLedgerRepository>();
        var tool = new GetTokenUsageSummaryTool(ledger, Substitute.For<ILogger<GetTokenUsageSummaryTool>>());

        var result = await tool.ExecuteAsync("{\"scope\":\"team\",\"scopeValue\":\"team-001\"}", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("team");
    }

    [Fact]
    public async Task GetTokenUsageSummaryTool_ShouldFailForTenantScopeWithInvalidGuid()
    {
        var ledger = Substitute.For<IAiTokenUsageLedgerRepository>();
        var tool = new GetTokenUsageSummaryTool(ledger, Substitute.For<ILogger<GetTokenUsageSummaryTool>>());

        var result = await tool.ExecuteAsync("{\"scope\":\"tenant\",\"scopeValue\":\"not-a-guid\"}", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("GUID");
    }

    // ── All 6 tools have unique names ─────────────────────────────────────

    [Fact]
    public void AllSixTools_ShouldHaveUniqueNames()
    {
        var catalogReader = Substitute.For<ICatalogGroundingReader>();
        var incidentReader = Substitute.For<IIncidentGroundingReader>();
        var ledger = Substitute.For<IAiTokenUsageLedgerRepository>();

        var tools = new IAgentTool[]
        {
            new ListServicesInfoTool(Substitute.For<ILogger<ListServicesInfoTool>>()),
            new GetServiceHealthTool(Substitute.For<ILogger<GetServiceHealthTool>>()),
            new ListRecentChangesTool(Substitute.For<ILogger<ListRecentChangesTool>>()),
            new GetContractDetailsTool(catalogReader, Substitute.For<ILogger<GetContractDetailsTool>>()),
            new SearchIncidentsTool(incidentReader, Substitute.For<ILogger<SearchIncidentsTool>>()),
            new GetTokenUsageSummaryTool(ledger, Substitute.For<ILogger<GetTokenUsageSummaryTool>>()),
        };

        tools.Select(t => t.Definition.Name).Should().OnlyHaveUniqueItems();
        tools.Should().HaveCount(6);
    }
}
